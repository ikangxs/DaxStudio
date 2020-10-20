﻿using System;
using Caliburn.Micro;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using DaxStudio.UI.Events;
using DaxStudio.Interfaces;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using DaxStudio.Common;
using DaxStudio.UI.Model;
using DaxStudio.UI.Interfaces;
using Serilog;
using DaxStudio.UI.Extensions;
using Humanizer;

namespace DaxStudio.UI.ViewModels
{
    [Export]
    public class HelpAboutViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDaxStudioHost _host;

        public IGlobalOptions Options { get; }

        [ImportingConstructor]
        public HelpAboutViewModel(IEventAggregator eventAggregator, IVersionCheck checker, IDaxStudioHost host, IGlobalOptions options) {
            _eventAggregator = eventAggregator;
            _host = host;
            Options = options;
            DisplayName = "About DaxStudio";
            CheckingUpdateStatus = true;
          
            NotifyOfPropertyChange(() => UpdateStatus);

            // start version check async
            VersionChecker = checker;
            VersionChecker.UpdateStartingCallback = this.VersionUpdateStarting;
            VersionChecker.UpdateCompleteCallback = this.VersionUpdateComplete;

            VersionChecker.PropertyChanged += VersionChecker_PropertyChanged;
            //Task.Run(() => 
            //    {
            //        this.VersionChecker.Update(); 
            //    })
            //    .ContinueWith((previous)=> {
            //        //  checking for exceptions and log them
            //        if (previous.IsFaulted)
            //        {
            //            Log.Error(previous.Exception, "{class} {method} {message}", nameof(HelpAboutViewModel), "ctor", $"Error updating version information: {previous.Exception.Message}");
            //            _eventAggregator.PublishOnUIThread(new OutputMessage(MessageType.Warning, "Unable to check for an updated release on github"));
            //            CheckingUpdateStatus = false;
            //            NotifyOfPropertyChange(() => CheckingUpdateStatus);
            //            return;
            //        }

            //        CheckingUpdateStatus = false;
            //        UpdateStatus = VersionChecker.VersionStatus;
            //        VersionIsLatest = VersionChecker.VersionIsLatest;
            //        DownloadUrl = VersionChecker.DownloadUrl;
            //        NotifyOfPropertyChange(() => VersionIsLatest);
            //        NotifyOfPropertyChange(() => DownloadUrl);
            //        NotifyOfPropertyChange(() => UpdateStatus);
            //        NotifyOfPropertyChange(() => CheckingUpdateStatus);
            //    },TaskScheduler.Default);
        }

        public void VersionUpdateStarting()
        {
            IsCheckRunning = true;
        }

        public void VersionUpdateComplete()
        {
            IsCheckRunning = false;

            NotifyOfPropertyChange(() => UpdateStatus);
            NotifyOfPropertyChange(() => LastChecked);
            VersionIsLatest = VersionChecker.VersionIsLatest;
            DownloadUrl = VersionChecker.DownloadUrl;
            NotifyOfPropertyChange(() => VersionIsLatest);
            NotifyOfPropertyChange(() => DownloadUrl);
        }

        void VersionChecker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VersionStatus")
            {

                VersionUpdateComplete();
            }
        }

        public string FullVersionNumber => System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3); 
        
        public string BuildNumber => System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(); 
        

        //[Import(typeof(IVersionCheck))]
        public IVersionCheck VersionChecker { get; private set; }

        public SortedList<string,string> ReferencedAssemblies
        {
            get
            {
                var l = new SortedList<string,string>();
                var ass = System.Reflection.Assembly.GetExecutingAssembly();
                foreach (var a in ass.GetReferencedAssemblies())
                {
                    if (!l.ContainsKey(a.Name))
                    {
                        l.Add(a.Name, a.Version.ToString());
                    }
                }
                return l; 
            }
        }
        public void Ok()
        {
            TryClose();
        }

        public bool CheckingUpdateStatus
        {
            get;
            set;
        }

        private bool _isCheckRunning;
        public bool IsCheckRunning { get => _isCheckRunning;
            set {
                _isCheckRunning = value;
                NotifyOfPropertyChange(() => IsCheckRunning);
            } 
        }

        public string UpdateStatus
        {
            get { 
                if (IsCheckRunning)
                {
                    return "Checking for updates...";
                }
                else
                {
                    if (VersionChecker.ServerVersion.IsNotSet()) return "(Unable to get version information)"; 

                    var versionComparison = VersionChecker.LocalVersion.CompareTo(VersionChecker.ServerVersion);
                    if (versionComparison > 0)  return $"(Ahead of Latest Version - {VersionChecker.ServerVersion.ToString(3)} )"; 
                    if (versionComparison == 0) return "You have the lastest Version"; 
                    
                    return $"(New Version available - {VersionChecker.ServerVersion})"; 

                }

            }

        }

        public string LastChecked => $"Last checked {Options.LastVersionCheckUTC.Humanize()}";
        public Uri DownloadUrl { get; private set; }
        public bool VersionIsLatest { get; private set; }

        public bool IsLoggingEnabled { get { return _host.DebugLogging; } }

        public string LogFolder { get { return @"file:///" + ApplicationPaths.LogPath; } }
    }

    public class ReferencedAssembly
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
