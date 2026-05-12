using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.CharacterData;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.Services.Events
{
    internal class GilService : IDisposable
    {
        private readonly ILog log;
        private readonly IFramework framework;
        private readonly CharacterDataService characterDataService;

        private long lastRun;

        internal GilService(ILog log, IFramework framework, CharacterDataService characterDataService) 
        {
            this.log = log;
            this.framework = framework;
            this.characterDataService = characterDataService;

            framework.Update += OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            long now = Environment.TickCount64;

            if (now - lastRun < 60_000)
                return;

            lastRun = now;

            CheckGil();
        }

        private async void CheckGil()
        {
            try
            {
                var total = characterDataService.GetTotalGil();
                FixedConfig.TotalGil = total;
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                FixedConfig.TotalGil = 0;
            }
            
        }

        public void Dispose()
        {
            framework.Update -= OnUpdate;
        }
    }
}
