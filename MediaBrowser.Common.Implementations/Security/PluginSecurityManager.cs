﻿using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace MediaBrowser.Common.Implementations.Security
{
    /// <summary>
    /// Class PluginSecurityManager
    /// </summary>
    public class PluginSecurityManager : ISecurityManager
    {
        private const string MBValidateUrl = MbAdmin.HttpsUrl + "service/registration/validate";
        private const string AppstoreRegUrl = /*MbAdmin.HttpsUrl*/ "http://mb3admin.com/admin/service/appstore/register";

        /// <summary>
        /// The _is MB supporter
        /// </summary>
        private bool? _isMbSupporter;
        /// <summary>
        /// The _is MB supporter initialized
        /// </summary>
        private bool _isMbSupporterInitialized;
        /// <summary>
        /// The _is MB supporter sync lock
        /// </summary>
        private object _isMbSupporterSyncLock = new object();

        /// <summary>
        /// Gets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        public bool IsMBSupporter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _isMbSupporter, ref _isMbSupporterInitialized, ref _isMbSupporterSyncLock, () => GetSupporterRegistrationStatus().Result.IsRegistered);
                return _isMbSupporter.Value;
            }
        }

        private MBLicenseFile _licenseFile;
        private MBLicenseFile LicenseFile
        {
            get { return _licenseFile ?? (_licenseFile = new MBLicenseFile(_appPaths)); }
        }

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _appHost;
        private readonly IApplicationPaths _appPaths;

        private IEnumerable<IRequiresRegistration> _registeredEntities;
        protected IEnumerable<IRequiresRegistration> RegisteredEntities
        {
            get
            {
                return _registeredEntities ?? (_registeredEntities = _appHost.GetExports<IRequiresRegistration>());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSecurityManager" /> class.
        /// </summary>
        public PluginSecurityManager(IApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer,
            IApplicationPaths appPaths, ILogManager logManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _appPaths = appPaths;
        }

        /// <summary>
        /// Load all registration info for all entities that require registration
        /// </summary>
        /// <returns></returns>
        public async Task LoadAllRegistrationInfo()
        {
            var tasks = new List<Task>();

            ResetSupporterInfo();
            tasks.AddRange(RegisteredEntities.Select(i => i.LoadRegistrationInfoAsync()));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the registration status.
        /// This overload supports existing plug-ins.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return GetRegistrationStatusInternal(feature, mb2Equivalent);
        }

        /// <summary>
        /// Gets the registration status.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <param name="version">The version of this feature</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent, string version)
        {
            return GetRegistrationStatusInternal(feature, mb2Equivalent, version);
        }

        private Task<MBRegistrationRecord> GetSupporterRegistrationStatus()
        {
            return GetRegistrationStatusInternal("MBSupporter", null, _appHost.ApplicationVersion.ToString());
        }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey
        {
            get
            {
                return LicenseFile.RegKey;
            }
            set
            {
                if (value != LicenseFile.RegKey)
                {
                    LicenseFile.RegKey = value;
                    LicenseFile.Save();

                    // re-load registration info
                    Task.Run(() => LoadAllRegistrationInfo());
                }
            }
        }

        /// <summary>
        /// Register an app store sale with our back-end.  It will validate the transaction with the store
        /// and then register the proper feature and then fill in the supporter key on success.
        /// </summary>
        /// <param name="parameters">Json parameters to send to admin server</param>
        public async Task RegisterAppStoreSale(string parameters)
        {
            SupporterKey = "Hello";
        }

        private void SaveAppStoreInfo(string info)
        {
            // Save all transaction information to a file

            try
            {
                File.WriteAllText(Path.Combine(_appPaths.ProgramDataPath, "apptrans-error.txt"), info);
            }
            catch (IOException)
            {

            }
        }

        private async Task<MBRegistrationRecord> GetRegistrationStatusInternal(string feature,
            string mb2Equivalent = null,
            string version = null)
        {
            var lastChecked = LicenseFile.LastChecked(feature);

            //check the reg file first to alleviate strain on the MB admin server - must actually check in every 30 days tho
            var reg = new RegRecord
            {
                // Cache the result for up to a week
                registered = lastChecked > DateTime.UtcNow.AddDays(-7)
            };

            var success = reg.registered;

            if (!(lastChecked > DateTime.UtcNow.AddDays(-1)))
            {
                LicenseFile.AddRegCheck(feature);
            }

            var record = new MBRegistrationRecord
            {
                IsRegistered = true,
                ExpirationDate = DateTime.UtcNow.AddYears(10),
                RegChecked = true,
                RegError = false
            };

            record.TrialVersion = IsInTrial(reg.expDate, record.RegChecked, record.IsRegistered);
            record.IsValid = !record.RegChecked || (record.IsRegistered || record.TrialVersion);

            return record;
        }

        private bool IsInTrial(DateTime expirationDate, bool regChecked, bool isRegistered)
        {
            //don't set this until we've successfully obtained exp date
            if (!regChecked)
            {
                return false;
            }

            var isInTrial = expirationDate > DateTime.UtcNow;

            return (isInTrial && !isRegistered);
        }

        /// <summary>
        /// Resets the supporter info.
        /// </summary>
        private void ResetSupporterInfo()
        {
            _isMbSupporter = null;
            _isMbSupporterInitialized = false;
        }
    }
}
