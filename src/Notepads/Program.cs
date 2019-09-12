﻿namespace Notepads
{
    using System.Collections.Generic;
    using System.Linq;
    using Notepads.Services;
    using Notepads.Settings;
    using Notepads.Utilities;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.Storage;

    public static class Program
    {
        public static bool IsFirstInstance { get; set; }

        private static IList<AppInstance> _instances;

        static void Main(string[] args)
        {
            _instances = AppInstance.GetInstances();

            if (_instances.Count == 0)
            {
                IsFirstInstance = true;
                ApplicationSettingsStore.Write("ActiveInstance", null);
            }

            IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

            if (activatedArgs is FileActivatedEventArgs)
            {
                RedirectOrCreateNewInstance();
            }
            else if (activatedArgs is CommandLineActivatedEventArgs cmdActivatedArgs)
            {
                RedirectOrCreateNewInstance();
            }
            else if (activatedArgs is ProtocolActivatedEventArgs protocolActivatedEventArgs)
            {
                LoggingService.LogInfo($"[Main] [ProtocolActivated] Protocol: {protocolActivatedEventArgs.Uri}");
                var protocol = NotepadsProtocolService.GetOperationProtocol(protocolActivatedEventArgs.Uri, out var context);
                if (protocol == NotepadsOperationProtocol.OpenNewInstance)
                {
                    OpenNewInstance();
                }
                else
                {
                    RedirectOrCreateNewInstance();
                }
            }
            else
            {
                // The platform might provide a recommended instance.
                if (AppInstance.RecommendedInstance != null)
                {
                    AppInstance.RecommendedInstance.RedirectActivationTo();
                }
                else
                {
                    RedirectOrCreateNewInstance();
                }
            }
        }

        private static void OpenNewInstance()
        {
            AppInstance.FindOrRegisterInstanceForKey(App.Id.ToString());
            App.IsFirstInstance = IsFirstInstance;
            Windows.UI.Xaml.Application.Start(p => new App());
            IsFirstInstance = false;
        }

        private static void RedirectOrCreateNewInstance()
        {
            var instance = (GetLastActiveInstance() ?? AppInstance.FindOrRegisterInstanceForKey(App.Id.ToString()));

            if (instance.IsCurrentInstance)
            {
                App.IsFirstInstance = IsFirstInstance;
                Windows.UI.Xaml.Application.Start(p => new App());
                IsFirstInstance = false;
            }
            else
            {
                instance.RedirectActivationTo();
            }
        }

        private static AppInstance GetLastActiveInstance()
        {
            var instances = AppInstance.GetInstances();
            if (instances.Count == 0)
            {
                return null;
            }
            else if (instances.Count == 1)
            {
                return instances.FirstOrDefault();
            }

            if (!(ApplicationSettingsStore.Read("ActiveInstance") is string activeInstance))
            {
                return null;
            }

            foreach (var appInstance in instances)
            {
                if (appInstance.Key == activeInstance)
                {
                    return appInstance;
                }
            }

            // activeInstance might be closed already, let's return the first instance in this case
            return instances.FirstOrDefault();
        }
    }
}