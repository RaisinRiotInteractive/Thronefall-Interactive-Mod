using System.Collections.Concurrent;
using System.Threading;
using TikTokLiveSharp.Client;
using TikTokLiveSharp.Client.Config;
using TikTokLiveSharp.Events.Objects;

namespace TikTokGiftsToEnemies
{
    public class TikTokConnectionManager
    {
        public static TikTokConnectionManager Instance { get; private set; }
        
        private TikTokLiveClient _client;
        private Thread _clientThread;
        private CancellationTokenSource _cts;
        private GiftEnemyMapper _mapper;
        private SpawnOrchestrator _orchestrator;
        private ConcurrentQueue<EnemySpawnRequest> _spawnQueue = new ConcurrentQueue<EnemySpawnRequest>();

        private long _totalLikes = 0;
        private long _lastProcessedLikes = 0;
        private ConcurrentDictionary<string, long> _userLikes = new ConcurrentDictionary<string, long>();

        public bool IsConnected => _client != null && _client.Connected;
        public string ConnectionStatus { get; private set; } = "Disconnected";

        public TikTokConnectionManager(GiftEnemyMapper mapper, SpawnOrchestrator orchestrator)
        {
            Instance = this;
            _mapper = mapper;
            _orchestrator = orchestrator;
        }

        public void Connect(string username)
        {
            if (string.IsNullOrEmpty(username)) return;

            Disconnect(); // Ensure clean state
            
            ConnectionStatus = "Connecting...";
            TikTokGiftsPlugin.Instance.Logger.LogInfo($"Connecting to TikTok LIVE: @{username}");

            try
            {
                var settings = new ClientSettings { PrintToConsole = false, LogLevel = LogLevel.Error };
                var newClient = new TikTokLiveClient(username, (string)null, settings, null);
                var newCts = new CancellationTokenSource();
                var token = newCts.Token;
                
                _client = newClient;
                _cts = newCts;

                newClient.OnGift += OnGiftReceived;
                newClient.OnLike += OnLikeReceived;
                newClient.OnFollow += OnFollowReceived;
                newClient.OnConnected += OnClientConnected;
                newClient.OnDisconnected += OnClientDisconnected;
                
                _clientThread = new Thread(() =>
                {
                    // Capture local ref to avoid races if _client is swapped
                    var activeClient = newClient;
                    activeClient.Run(token, ex =>
                    {
                        if (activeClient == _client)
                        {
                            var fullError = $"Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}";
                            if (ex.InnerException != null)
                                fullError += $"\n\nInner Type: {ex.InnerException.GetType().FullName}\nInner Message: {ex.InnerException.Message}\nInner Stack Trace:\n{ex.InnerException.StackTrace}";
                            TikTokGiftsPlugin.Instance.Logger.LogError($"TikTok LIVE Error: {fullError}");
                            ConnectionStatus = "Error";
                        }
                    }, retryConnection: false);
                });
                _clientThread.IsBackground = true;
                _clientThread.Start();
            }
            catch (System.Exception ex)
            {
                ConnectionStatus = "Error";
                var fullError = $"Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}";
                TikTokGiftsPlugin.Instance.Logger.LogError($"Failed to initiate connection: {fullError}");
            }
        }

        private void OnClientConnected(TikTokBaseClient client, bool b)
        {
            if (client == _client)
            {
                ConnectionStatus = "Connected";
                TikTokGiftsPlugin.Instance.Logger.LogInfo("Connected!");
            }
        }

        private void OnClientDisconnected(TikTokBaseClient client, bool b)
        {
            if (client == _client)
            {
                ConnectionStatus = "Disconnected";
                TikTokGiftsPlugin.Instance.Logger.LogInfo("Disconnected.");
            }
        }

        private void OnGiftReceived(TikTokBaseClient client, TikTokGift gift)
        {
            if (client != _client) return;
            ProcessGift(gift);
        }

        private void OnLikeReceived(TikTokBaseClient client, object like)
        {
            if (client != _client) return;
            ProcessLikes((dynamic)like);
        }

        private void OnFollowReceived(TikTokBaseClient client, object follow)
        {
            if (client != _client) return;
            TikTokGiftsPlugin.Instance.Logger.LogInfo("[Follow] OnFollowReceived event fired.");
            ProcessFollow((dynamic)follow);
        }

        private void ProcessFollow(dynamic follow)
        {
            try
            {
                // ── Debug: Inspect the follow object ───────────────────────
                var type = follow.GetType();
                TikTokGiftsPlugin.Instance.Logger.LogInfo($"[Follow] Inspecting {type.FullName}...");
                foreach (var m in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    TikTokGiftsPlugin.Instance.Logger.LogInfo($"[Follow] Property: {m.Name}");
                }
                foreach (var f in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    TikTokGiftsPlugin.Instance.Logger.LogInfo($"[Follow] Field: {f.Name}");
                }
                // ──────────────────────────────────────────────────────────

                // Based on common library patterns, it might be 'User' instead of 'Sender'
                User user = null;
                try { user = follow.User; } catch { }
                if (user == null) try { user = follow.Sender; } catch { }
                
                string name = user?.NickName ?? "unknown";
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[Follow] New follower received: {name}");

                var requests = _mapper.MapFollow(user);
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[Follow] Mapped to {requests.Count} spawn requests.");

                foreach (var request in requests)
                {
                    TikTokGiftsPlugin.Instance.Logger.LogInfo(
                        $"[Follow] Enqueue spawn: prefab='{request.PrefabName}' count={request.Count} for {name}");
                    _spawnQueue.Enqueue(request);
                }
            }
            catch (System.Exception ex)
            {
                TikTokGiftsPlugin.Instance.Logger.LogError($"[Follow] Error processing follow: {ex}");
            }
        }

        private void ProcessLikes(dynamic like)
        {
            string userId = like.Sender?.UniqueId ?? like.Sender?.NickName ?? "unknown";
            int count = (int)like.Count;
            
            // Update per-user total
            long userTotal = _userLikes.AddOrUpdate(userId, count, (id, old) => old + count);
            long userBefore = userTotal - count;

            TikTokGiftsPlugin.Instance.Logger.LogInfo(
                $"[Like] User {userId} likes: {userTotal} (+{count})");

            var requests = _mapper.MapLikes(userTotal, userBefore, (User)like.Sender);
            foreach (var request in requests)
            {
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[Like] Mapped to prefab='{request.PrefabName}' count={request.Count} for {userId}");
                _spawnQueue.Enqueue(request);
            }

            // Still track stream total for logging
            _totalLikes = (long)like.TotalLikes;
        }


        private void ProcessGift(TikTokGift gift)
        {
            string giftName = gift.Gift?.Name ?? "(null)";
            int amount      = (int)System.Math.Max(1, gift.Amount);
            int diamonds    = (gift.Gift?.DiamondCost ?? 0) * amount;
            TikTokGiftsPlugin.Instance.Logger.LogInfo(
                $"[Gift] '{giftName}' x{amount} ({diamonds} diamonds) from {gift.Sender?.NickName}");

            var request = _mapper.Map(gift);
            if (request != null)
            {
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[Gift] Mapped to prefab='{request.PrefabName}' count={request.Count}");
                _spawnQueue.Enqueue(request);
            }
            else
            {
                TikTokGiftsPlugin.Instance.Logger.LogWarning(
                    $"[Gift] No rule matched for '{giftName}' ({diamonds} diamonds)");
            }
        }

        public void Disconnect()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }

            if (_client != null)
            {
                _client.OnGift -= OnGiftReceived;
                _client.OnLike -= OnLikeReceived;
                _client.OnFollow -= OnFollowReceived;
                _client.OnConnected -= OnClientConnected;
                _client.OnDisconnected -= OnClientDisconnected;
                try
                {
                    _client.Stop().GetAwaiter().GetResult();
                }
                catch { /* Ignore stop errors during disconnect */ }
            }

            if (_clientThread != null && _clientThread.IsAlive)
            {
                _clientThread.Join(1000); // 1 second bounded join
            }
            
            _client = null;
            _clientThread = null;
            
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
            
            ConnectionStatus = "Disconnected";
        }

        public void Tick()
        {
            while (_spawnQueue.TryDequeue(out var request))
            {
                _orchestrator.HandleSpawnRequest(request);
            }
        }
    }
}
