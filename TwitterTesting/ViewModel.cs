﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Codeplex.Data;
using Codeplex.OAuth;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows;

namespace TwitterTesting
{
    public class ViewModel : INotifyPropertyChanged
    {
        private const string ConsumerKey = "u4DWxeq3bGfLCJKNwkSyjQ";
        private const string ConsumerSecret = "0kETRYnHiqXzyaG3B0ud5N90Ffwi6FuRsvOPbB9fBY";
        private RequestToken requestToken;
        private AccessToken accessToken;

        public ViewModel(AccessToken token): this()
        {
            accessToken = token;
        }

        public ReadOnlyObservableCollection<TimelineItemViewModel> Tweet
        {
            get
            {
                if (StreamingApi != null)
                    return (ReadOnlyObservableCollection<TimelineItemViewModel>)StreamingApi.Where(x => x.text()).Select(x => new TimelineItemViewModel(x)).ObserveOnDispatcher();
                return null;
            }
        }

        public string AuthorizeUrl
        {
            get;
            private set;
        }

        public string PinCode
        {
            get;
            set;
        }

        public string UserId
        {
            get;
            private set;
        }

        public string ScreenName
        {
            get;
            private set;
        }

        public IObservable<dynamic> StreamingApi
        {
            get;
            private set;
        }

        private HashSet<int> friendList;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler CanGetTimeline;
        
        private Lazy<Command> _getRequestToken;
        public ICommand GetRequestToken { get { return _getRequestToken.Value; } }
        private Lazy<Command> _getAccessToken;
        public ICommand GetAccessToken { get { return _getAccessToken.Value; } }
        private Lazy<Command> _startGetTimeline;
        public ICommand StartGetTimeline { get { return _startGetTimeline.Value; } }

        /// <summary>
        /// タイムラインで表示する1アイテムを表すViewModel
        /// </summary>
        public class TimelineItemViewModel
        {
            private dynamic _item;
            public TimelineItemViewModel(dynamic item)
            {
                _item = item;
            }

            public string Text
            {
                get { return _item.text; }
            }

            public string Name
            {
                get { return _item.user.screen_name; }
            }

            public override string ToString()
            {
                return _item.ToString();
            }
        }


        public ViewModel()
        {
            _getRequestToken = new Lazy<Command>(() =>
                new Command(_ =>
                {
                    var authorizer = new OAuthAuthorizer(ConsumerKey, ConsumerSecret);
                    authorizer.GetRequestToken("http://twitter.com/oauth/request_token")
                    .Select(res => res.Token)
                    .ObserveOnDispatcher()
                    .Subscribe(token =>
                    {
                        requestToken = token;
                        AuthorizeUrl = authorizer.BuildAuthorizeUrl("http://twitter.com/oauth/authorize", token);
                    }, e => MessageBox.Show(e.ToString()));
                })
            );
            _getAccessToken = new Lazy<Command>(() =>
                new Command(_ =>
                {
                    new OAuthAuthorizer(ConsumerKey, ConsumerSecret).GetAccessToken("http://twitter.com/oauth/access_token", requestToken, PinCode)
                        .ObserveOnDispatcher()
                        .Subscribe(res =>
                        {
                            UserId = res.ExtraData["user_id"].First();
                            ScreenName = res.ExtraData["screen_name"].First();
                            accessToken = res.Token;
                        }, e => MessageBox.Show(e.ToString()));
                    CanGetTimeline(this, new EventArgs());
                })
            );

            _startGetTimeline = new Lazy<Command>(() =>
            {
                return new Command(_ =>
                {
                    StreamingApi = new OAuthClient(ConsumerKey, ConsumerSecret, accessToken) { Url = "https://userstream.twitter.com/2/user.json" }
                        .GetResponseLines()
                        .Where(s => !string.IsNullOrWhiteSpace(s)) // filter invalid data
                        .Select(s => DynamicJson.Parse(s));
                    StreamingApi.Take(1).Subscribe(x => friendList = new HashSet<int>(x.friends.Select((Func<double, int>)(id => (int)id))), e => MessageBox.Show(e.ToString())); ;
                    StreamingApi.Subscribe(x => this.PropertyChanged(this, new PropertyChangedEventArgs("StreamingApi")), e => MessageBox.Show(e.ToString())); ;
                });
            },false);
            CanGetTimeline += new EventHandler((sender, e) => _startGetTimeline.Value.IsCanExecute = true);
        }
    }
}