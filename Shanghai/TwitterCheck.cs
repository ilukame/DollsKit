﻿using CoreTweet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;

namespace Shanghai
{
    class TwitterCheck
    {
        private static readonly string[] BlackList = {
            "Mewdra", "nippy", "metto0226",
        };
        private static readonly int SettingMax = 100;
        private readonly ReadOnlyCollection<string> BlackWords, WhiteWords;

        public TwitterCheck()
        {
            var settings = ConfigurationManager.AppSettings;

            var blackWords = new List<string>();
            var whiteWords = new List<string>();
            for (int i = 1; i <= SettingMax; i++)
            {
                string black = settings["twitter.blackwords." + i];
                if (black != null)
                {
                    foreach (var elem in black.Split(','))
                    {
                        blackWords.Add(elem);
                    }
                }
                string white = settings["twitter.whitewords." + i];
                if (white != null)
                {
                    foreach (var elem in white.Split(','))
                    {
                        whiteWords.Add(elem);
                    }
                }
            }
            BlackWords = blackWords.AsReadOnly();
            WhiteWords = whiteWords.AsReadOnly();
            Log.Trace.TraceEvent(TraceEventType.Information, 0,
                "{0} black words loaded", BlackWords.Count);
            Log.Trace.TraceEvent(TraceEventType.Information, 0,
               "{0} white words loaded", WhiteWords.Count);
        }

        private bool IsBlack(Status status, long masterId)
        {
            // not master
            if (status.User.Id == masterId)
            {
                return false;
            }
            if (Array.IndexOf(BlackList, status.User.Name) < 0)
            {
                return false;
            }

            bool black = false;
            foreach (var word in BlackWords)
            {
                black = black || status.Text.Contains(word);
            }

            const int AfterHour = 21;
            const int BeforeHour = 5;
            DateTimeOffset localTime = status.CreatedAt.ToLocalTime();
            black = black && (localTime.Hour >= AfterHour || localTime.Hour <= BeforeHour);

            return black;
        }

        private bool IsWhite(Status status)
        {
            // master only
            if (status.User.Id != TwitterManager.MasterTokens.UserId)
            {
                return false;
            }

            bool white = false;
            foreach (var word in WhiteWords)
            {
                white = white || status.Text.Contains(word);
            }

            return white;
        }

        public void CheckBlack(TaskServer server, string taskName)
        {
            const int SearchCount = 200;
            long masterId = TwitterManager.MasterTokens.Account.VerifyCredentials().Id ?? 0;

            var timeline = TwitterManager.MasterTokens.Statuses.HomeTimeline(count: SearchCount);
            foreach (var status in timeline)
            {
                if (IsBlack(status, masterId))
                {
                    if (!(status.IsFavorited ?? false))
                    {
                        Log.Trace.TraceEvent(TraceEventType.Information, 0,
                            "[{0}] Find black: {1} - {2}", taskName, status.User.Name, status.Text);
                        TwitterManager.Favorite(status.Id);
                        TwitterManager.Update(
                            string.Format("@{0} ブラック", status.User.Name),
                            status.Id);
                    }
                    else
                    {
                        Log.Trace.TraceEvent(TraceEventType.Information, 0, "Skip");
                    }
                }
            }
        }

        public void CheckMention(TaskServer server, string taskName)
        {
            const int SearchCount = 200;

            var timeline = TwitterManager.Tokens.Statuses.MentionsTimeline(count: SearchCount);
            foreach (var status in timeline)
            {
                if (!(status.IsFavorited ?? false))
                {
                    Log.Trace.TraceEvent(TraceEventType.Information, 0,
                        "[{0}] Find mention: {1} - {2}", taskName, status.User.Name, status.Text);
                    TwitterManager.Favorite(status.Id);
                    TwitterManager.Update(
                        string.Format("@{0} バカジャネーノ", status.User.Name),
                        status.Id);
                }
            }
        }
    }
}
