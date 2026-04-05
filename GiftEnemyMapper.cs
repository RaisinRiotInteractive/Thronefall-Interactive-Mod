using System;
using System.Collections.Generic;
using TikTokLiveSharp.Events.Objects;

namespace TikTokGiftsToEnemies
{
    public class GiftEnemyMapper
    {
        public EnemySpawnRequest Map(TikTokGift gift)
        {
            string name  = gift.Gift?.Name ?? "";
            int amount   = (int)System.Math.Max(1, gift.Amount);
            int diamonds = (gift.Gift?.DiamondCost ?? 0) * amount;

            string prefab = null;
            int finalCount = 0;

            // 1. Try to match by gift name
            var giftRule = FindGiftRule(name);
            if (giftRule != null)
            {
                prefab = giftRule.Value.prefabName;
                finalCount = giftRule.Value.count * amount;
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[GiftStack] Matched '{name}' rule. Count {giftRule.Value.count} * Amount {amount} = {finalCount}");
            }
            else
            {
                // 2. Fallback to diamond (coin) rules
                var coinRule = FindCoinRule(diamonds);
                if (coinRule != null)
                {
                    prefab = coinRule.Value.prefabName;
                    int threshold = coinRule.Value.threshold;
                    if (threshold > 0)
                    {
                        int multiplier = diamonds / threshold;
                        finalCount = coinRule.Value.count * multiplier;
                        TikTokGiftsPlugin.Instance.Logger.LogInfo(
                            $"[GiftStack] Matched Coin rule ({threshold}). Diamonds {diamonds} / Threshold {threshold} = {multiplier}x multiplier. Total count: {finalCount}");
                    }
                }
            }

            if (prefab == null || finalCount <= 0)
            {
                TikTokGiftsPlugin.Instance.Logger.LogWarning(
                    $"[GiftStack] No rule matched for '{name}' ({diamonds} diamonds)");
                return null;
            }

            string picUrl = GetBestAvatarUrl(gift.Sender);

            return new EnemySpawnRequest
            {
                PrefabName    = prefab,
                Count         = finalCount,
                SenderName    = gift.Sender?.NickName ?? "unknown",
                GiftName      = name,
                TotalDiamonds = diamonds,
                ProfilePicUrl = picUrl
            };
        }

        public List<EnemySpawnRequest> MapLikes(long totalLikes, long lastProcessedLikes, User sender)
        {
            var requests = new List<EnemySpawnRequest>();
            string name = sender?.NickName ?? "Liker";

            string picUrl = GetBestAvatarUrl(sender);

            foreach (var rule in FindLikeRules())
            {
                // Calculate how many times we've crossed this threshold since last time
                long threshold = rule.likes;
                if (threshold <= 0) continue;

                long timesBefore = lastProcessedLikes / threshold;
                long timesNow    = totalLikes / threshold;
                long newSpawns   = timesNow - timesBefore;

                if (newSpawns > 0)
                {
                    requests.Add(new EnemySpawnRequest
                    {
                        PrefabName = rule.prefabName,
                        Count      = (int)(rule.count * newSpawns),
                        SenderName = name,
                        GiftName   = "Likes",
                        TotalDiamonds = 0,
                        ProfilePicUrl = picUrl
                    });
                }
            }

            return requests;
        }

        public List<EnemySpawnRequest> MapFollow(long totalFollows, long lastProcessedFollows, User sender)
        {
            var requests = new List<EnemySpawnRequest>();
            string name = sender?.NickName ?? "Follower";

            string picUrl = GetBestAvatarUrl(sender);

            foreach (var rule in FindFollowRules())
            {
                long threshold = rule.follows;
                if (threshold <= 0) continue;

                long timesBefore = lastProcessedFollows / threshold;
                long timesNow    = totalFollows / threshold;
                long newSpawns   = timesNow - timesBefore;

                if (newSpawns > 0)
                {
                    requests.Add(new EnemySpawnRequest
                    {
                        PrefabName    = rule.prefabName,
                        Count         = (int)(rule.count * newSpawns),
                        SenderName    = name,
                        GiftName      = "Follow",
                        TotalDiamonds = 0,
                        ProfilePicUrl = picUrl
                    });
                }
            }

            return requests;
        }

        private string GetBestAvatarUrl(User sender)
        {
            if (sender == null) return null;

            try
            {
                var type = sender.GetType();

                foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    try
                    {
                        var val = p.GetValue(sender);
                        if (val == null) continue;
                        string url = ExtractUrlFromObject(val, p.Name);
                        if (url != null) return url;
                    }
                    catch { }
                }

                foreach (var f in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    try
                    {
                        var val = f.GetValue(sender);
                        if (val == null) continue;
                        string url = ExtractUrlFromObject(val, f.Name);
                        if (url != null) return url;
                    }
                    catch { }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractUrlFromObject(object val, string memberName)
        {
            if (val == null) return null;

            // If it's a Picture object
            if (val is TikTokLiveSharp.Events.Objects.Picture pic)
            {
                if (pic.Urls != null && pic.Urls.Count > 0)
                {
                    string fallback = null;
                    foreach (var u in pic.Urls)
                    {
                        if (string.IsNullOrEmpty(u)) continue;
                        string cleanUrl = u.StartsWith("//") ? "https:" + u : u;
                        if (!cleanUrl.ToLower().Contains(".webp"))
                            return cleanUrl;
                        if (fallback == null) fallback = cleanUrl;
                    }
                    if (fallback != null) return fallback;
                }
            }
            else if (val is string s)
            {
                string cleanS = s.StartsWith("//") ? "https:" + s : s;
                if (cleanS.StartsWith("http") && (memberName.Contains("Avatar") || memberName.Contains("Image") || memberName.Contains("Profile")))
                    return cleanS;
            }
            return null;
        }


        private List<(long likes, string prefabName, int count)> FindLikeRules()
        {
            var result = new List<(long, string, int)>();
            foreach (var entry in ParseRules(PluginConfig.LikeRules.Value))
            {
                if (long.TryParse(entry.key, out long threshold))
                {
                    result.Add((threshold, entry.prefab, entry.count));
                }
            }
            return result;
        }

        private List<(long follows, string prefabName, int count)> FindFollowRules()
        {
            var result = new List<(long, string, int)>();
            foreach (var entry in ParseRules(PluginConfig.FollowRules.Value))
            {
                if (long.TryParse(entry.key, out long threshold))
                {
                    result.Add((threshold, entry.prefab, entry.count));
                }
            }
            return result;
        }

        private (string prefabName, int count)? FindGiftRule(string giftName)
        {
            if (string.IsNullOrEmpty(giftName)) return null;

            foreach (var entry in ParseRules(PluginConfig.GiftRules.Value))
            {
                if (entry.key.Equals(giftName, StringComparison.OrdinalIgnoreCase))
                    return (entry.prefab, entry.count);
            }
            return null;
        }

        private (string prefabName, int count, int threshold)? FindCoinRule(int diamonds)
        {
            int bestThreshold = -1;
            (string prefabName, int count, int threshold)? best = null;

            foreach (var entry in ParseRules(PluginConfig.CoinRules.Value))
            {
                if (int.TryParse(entry.key, out int threshold) &&
                    diamonds >= threshold && threshold > bestThreshold)
                {
                    bestThreshold = threshold;
                    best = (entry.prefab, entry.count, threshold);
                }
            }
            return best;
        }

        // Parses "Key:PrefabName:Count;..." into a list of (key, prefab, count)
        private static List<(string key, string prefab, int count)> ParseRules(string raw)
        {
            var result = new List<(string, string, int)>();
            if (string.IsNullOrEmpty(raw)) return result;

            foreach (var part in raw.Split(';'))
            {
                var seg = part.Trim().Split(':');
                if (seg.Length == 3 &&
                    int.TryParse(seg[2].Trim(), out int count) &&
                    seg[0].Trim().Length > 0 &&
                    seg[1].Trim().Length > 0)
                {
                    result.Add((seg[0].Trim(), seg[1].Trim(), count));
                }
            }
            return result;
        }
    }
}
