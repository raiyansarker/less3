﻿using System;
using System.Collections.Generic;
using System.Text;

using S3ServerInterface;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Authentication manager.
    /// </summary>
    internal class AuthManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;

        #endregion

        #region Constructors-and-Factories

        internal AuthManager()
        {

        }

        internal AuthManager(
            Settings settings, 
            LoggingModule logging, 
            ConfigManager config, 
            BucketManager buckets)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
        }

        #endregion

        #region Internal-Methods
        
        internal bool Authenticate(
            S3Request req, 
            out User user, 
            out Credential cred)
        {
            user = null;
            cred = null;
            if (req == null) throw new ArgumentNullException(nameof(req));

            if (String.IsNullOrEmpty(req.AccessKey)) return false;
            if (!_Config.GetUserByAccessKey(req.AccessKey, out user)) return false;
            if (!_Config.GetCredentialByAccessKey(req.AccessKey, out cred)) return false;

            return true;
        }

        internal bool AuthorizeAdminRequest(
            RequestType reqType,
            S3Request req,
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req)); 
            bool allowed = false;

            string logMsg = 
                "AuthManager AuthorizeAdminRequest " + 
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                if (reqType != RequestType.Admin)
                    throw new ArgumentException("Unsupported request type for this method: " + reqType.ToString());

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication) 
                            _Logging.Info(
                                "AuthManager AuthorizeAdminRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl); 

                        result = AuthResult.AdminAuthorized;
                        allowed = true; 
                    }
                }

                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        internal bool AuthorizeServiceRequest(
            RequestType reqType,
            S3Request req,
            User user,
            Credential cred, 
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            bool allowed = false;

            string logMsg =
                "AuthManager AuthorizeServiceRequest " +
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                #region Check-Request-Type

                if (reqType != RequestType.ServiceListBuckets)
                    throw new ArgumentException("Unsupported request type for this method: " + reqType.ToString());

                #endregion

                #region Check-for-Admin-API-Key

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication)
                            _Logging.Info(
                                "AuthManager AuthorizeServiceRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl);

                        result = AuthResult.AdminAuthorized;
                        allowed = true;
                        return true;
                    }
                }

                #endregion
                 
                // user and cred are already populated
                result = AuthResult.Authenticated;
                allowed = true; 
                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        internal bool AuthorizeBucketRequest(
            RequestType reqType,
            S3Request req,
            User user,
            Credential cred,
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            bool allowed = false;

            string logMsg =
                "AuthManager AuthorizeBucketRequest " +
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                #region Check-Request-Type

                if (reqType != RequestType.BucketWrite) 
                    throw new ArgumentException("Unsupported request type for this method: " + reqType.ToString());

                #endregion

                #region Check-for-Admin-API-Key

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication)
                            _Logging.Info(
                                "AuthManager AuthorizeBucketRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl);

                        result = AuthResult.AdminAuthorized;
                        allowed = true;
                        return true;
                    }
                }

                #endregion
                                 
                // user and cred are already populated
                result = AuthResult.Authenticated;
                allowed = true; 

                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        internal bool AuthorizeBucketRequest(
            RequestType reqType,
            S3Request req,
            User user,
            Credential cred,
            BucketConfiguration bucket,
            BucketClient client,
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req)); 
            bool allowed = false;

            string logMsg =
                "AuthManager AuthorizeBucketRequest " +
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                 req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                #region Gather-ACLs

                List<BucketAcl> bucketAcls = new List<BucketAcl>();
                client.GetBucketAcl(out bucketAcls);

                #endregion

                #region Check-for-Admin-API-Key

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication)
                            _Logging.Info(
                                "AuthManager AuthorizeBucketRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl);

                        result = AuthResult.AdminAuthorized;
                        allowed = true;
                        return true;
                    }
                }

                #endregion

                #region Check-for-Bucket-Global-Config

                switch (reqType)
                {
                    case RequestType.BucketExists:
                    case RequestType.BucketRead:
                    case RequestType.BucketReadVersioning:
                    case RequestType.BucketReadVersions:
                        if (bucket.EnablePublicRead)
                        {
                            result = AuthResult.PermitBucketGlobalConfig;
                            allowed = true;
                            return allowed;
                        }
                        break;
                         
                    case RequestType.BucketDeleteTags: 
                    case RequestType.BucketWriteTags:
                    case RequestType.BucketWriteVersioning:
                        if (bucket.EnablePublicWrite)
                        {
                            result = AuthResult.PermitBucketGlobalConfig;
                            allowed = true;
                            return allowed;
                        }
                        break; 
                }

                #endregion

                #region Check-for-Bucket-AllUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.BucketExists:
                        case RequestType.BucketRead:
                        case RequestType.BucketReadVersioning:
                        case RequestType.BucketReadVersions:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.BucketReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.BucketDelete:
                        case RequestType.BucketDeleteTags:
                        case RequestType.BucketWrite:
                        case RequestType.BucketWriteTags:
                        case RequestType.BucketWriteVersioning:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWrite || b.FullControl));
                            break; 

                        case RequestType.BucketWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break;
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAllUsersAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Auth-Material

                if (user == null || cred == null)
                {
                    result = AuthResult.AuthenticationRequired;
                    allowed = false;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-Owner

                if (bucket.OwnerGUID.Equals(user.GUID))
                {
                    result = AuthResult.PermitBucketOwnership;
                    allowed = true;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-AuthenticatedUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.BucketExists:
                        case RequestType.BucketRead:
                        case RequestType.BucketReadVersioning:
                        case RequestType.BucketReadVersions:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.BucketReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.BucketDelete:
                        case RequestType.BucketDeleteTags:
                        case RequestType.BucketWrite:
                        case RequestType.BucketWriteTags:
                        case RequestType.BucketWriteVersioning:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.BucketWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break;
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAuthUserAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Bucket-User-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.BucketExists:
                        case RequestType.BucketRead:
                        case RequestType.BucketReadVersioning:
                        case RequestType.BucketReadVersions:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.BucketReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.BucketDelete:
                        case RequestType.BucketDeleteTags:
                        case RequestType.BucketWrite:
                        case RequestType.BucketWriteTags:
                        case RequestType.BucketWriteVersioning:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.BucketWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWriteAcp || b.FullControl));
                            break;
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketUserAcl;
                        return allowed;
                    }
                }

                #endregion
                 
                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        internal bool AuthorizeObjectRequest(
            RequestType reqType,
            S3Request req,
            User user,
            Credential cred,
            BucketConfiguration bucket,
            BucketClient client, 
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req)); 
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            if (client == null) throw new ArgumentNullException(nameof(client));
            bool allowed = false;

            string logMsg =
                "AuthManager AuthorizeObjectRequest " +
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                #region Check-Request-Type

                if (reqType != RequestType.ObjectWrite)
                    throw new ArgumentException("Unsupported request type for this method: " + reqType.ToString());

                #endregion

                #region Gather-ACLs

                List<BucketAcl> bucketAcls = new List<BucketAcl>();
                client.GetBucketAcl(out bucketAcls);

                #endregion

                #region Check-for-Admin-API-Key

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication)
                            _Logging.Info(
                                "AuthManager AuthorizeObjectRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl);

                        result = AuthResult.AdminAuthorized;
                        allowed = true;
                        return true;
                    }
                }

                #endregion

                #region Check-for-Bucket-Global-Config

                if (bucket.EnablePublicWrite)
                {
                    result = AuthResult.PermitBucketGlobalConfig;
                    allowed = true;
                    return allowed;
                }
                 
                #endregion

                #region Check-for-Bucket-AllUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                { 
                    allowed = bucketAcls.Exists(
                        b => !String.IsNullOrEmpty(b.UserGroup)
                        && b.UserGroup.Contains("AllUsers")
                        && (b.PermitWrite || b.FullControl));  

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAllUsersAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Auth-Material

                if (user == null || cred == null)
                {
                    result = AuthResult.AuthenticationRequired;
                    allowed = false;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-Owner

                if (bucket.OwnerGUID.Equals(user.GUID))
                {
                    result = AuthResult.PermitBucketOwnership;
                    allowed = true;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-AuthenticatedUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                { 
                    allowed = bucketAcls.Exists(
                        b => !String.IsNullOrEmpty(b.UserGroup)
                        && b.UserGroup.Contains("AuthenticatedUsers")
                        && (b.PermitWrite || b.FullControl)); 
                     
                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAuthUserAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Bucket-User-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                { 
                    allowed = bucketAcls.Exists(
                        b => !String.IsNullOrEmpty(b.UserGUID)
                        && b.UserGUID.Equals(user.GUID)
                        && (b.PermitWrite || b.FullControl)); 

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketUserAcl;
                        return allowed;
                    }
                }

                #endregion

                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        internal bool AuthorizeObjectRequest(
            RequestType reqType,
            S3Request req,
            User user,
            Credential cred,
            BucketConfiguration bucket,
            BucketClient client,
            Obj obj,
            out AuthResult result)
        {
            result = AuthResult.Denied;
            if (req == null) throw new ArgumentNullException(nameof(req)); 
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            bool allowed = false;

            string logMsg =
                "AuthManager AuthorizeObjectRequest " +
                req.SourceIp + ":" + req.SourcePort + " " +
                reqType.ToString() + " " +
                req.Method.ToString() + " " + req.RawUrl + " ";

            try
            {
                #region Get-Version-ID

                long versionId = 1;
                if (req.Querystring != null && req.Querystring.ContainsKey("versionId"))
                {
                    if (!Int64.TryParse(req.Querystring["versionId"], out versionId))
                    {
                        versionId = 1;
                    }
                }

                #endregion

                #region Gather-ACLs

                List<BucketAcl> bucketAcls = new List<BucketAcl>();
                client.GetBucketAcl(out bucketAcls);

                List<ObjectAcl> objectAcls = new List<ObjectAcl>();
                client.GetObjectAcl(req.Key, versionId, out objectAcls);

                #endregion

                #region Check-for-Admin-API-Key

                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        if (_Settings.Debug.Authentication)
                            _Logging.Info(
                                "AuthManager AuthorizeObjectRequest admin API key in use: " +
                                req.SourceIp + ":" + req.SourcePort + " " +
                                reqType.ToString() + " " +
                                req.Method.ToString() + " " + req.RawUrl);

                        result = AuthResult.AdminAuthorized;
                        allowed = true;
                        return true;
                    }
                }

                #endregion

                #region Check-for-Bucket-Global-Config

                switch (reqType)
                {
                    case RequestType.ObjectExists:
                    case RequestType.ObjectRead:
                    case RequestType.ObjectReadLegalHold:
                    case RequestType.ObjectReadRange:
                    case RequestType.ObjectReadRetention:
                    case RequestType.ObjectReadTags:
                        if (bucket.EnablePublicRead) allowed = true;
                        break;
                         
                    case RequestType.ObjectDelete:
                    case RequestType.ObjectDeleteMultiple:
                    case RequestType.ObjectDeleteTags:
                    case RequestType.ObjectWriteLegalHold:
                    case RequestType.ObjectWriteRetention:
                    case RequestType.ObjectWriteTags:
                        if (bucket.EnablePublicWrite) allowed = true;
                        break;  
                }

                if (allowed)
                {
                    result = AuthResult.PermitBucketGlobalConfig;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-AllUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }
                     
                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAllUsersAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Object-AllUsers-ACL

                if (objectAcls != null && objectAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AllUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitObjectAllUsersAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Auth-Material

                if (user == null || cred == null)
                {
                    result = AuthResult.AuthenticationRequired;
                    allowed = false;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-Owner

                if (bucket.OwnerGUID.Equals(user.GUID))
                {
                    result = AuthResult.PermitBucketOwnership;
                    allowed = true;
                    return allowed;
                }

                #endregion

                #region Check-for-Object-Owner

                if (obj.Owner.Equals(user.GUID))
                {
                    result = AuthResult.PermitObjectOwnership;
                    allowed = true;
                    return allowed;
                }

                #endregion

                #region Check-for-Bucket-AuthenticatedUsers-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketAuthUserAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Object-AuthenticatedUsers-ACL

                if (objectAcls != null && objectAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGroup)
                                && b.UserGroup.Contains("AuthenticatedUsers")
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitObjectAuthUserAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Bucket-User-ACL

                if (bucketAcls != null && bucketAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = bucketAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitBucketUserAcl;
                        return allowed;
                    }
                }

                #endregion

                #region Check-for-Object-User-ACL

                if (objectAcls != null && objectAcls.Count > 0)
                {
                    switch (reqType)
                    {
                        case RequestType.ObjectExists:
                        case RequestType.ObjectRead:
                        case RequestType.ObjectReadLegalHold:
                        case RequestType.ObjectReadRange:
                        case RequestType.ObjectReadRetention:
                        case RequestType.ObjectReadTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitRead || b.FullControl));
                            break;

                        case RequestType.ObjectReadAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitReadAcp || b.FullControl));
                            break;

                        case RequestType.ObjectDelete:
                        case RequestType.ObjectDeleteMultiple:
                        case RequestType.ObjectDeleteTags:
                        case RequestType.ObjectWriteLegalHold:
                        case RequestType.ObjectWriteRetention:
                        case RequestType.ObjectWriteTags:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWrite || b.FullControl));
                            break;

                        case RequestType.ObjectWriteAcl:
                            allowed = objectAcls.Exists(
                                b => !String.IsNullOrEmpty(b.UserGUID)
                                && b.UserGUID.Equals(user.GUID)
                                && (b.PermitWriteAcp || b.FullControl));
                            break; 
                    }

                    if (allowed)
                    {
                        result = AuthResult.PermitObjectUserAcl;
                        return allowed;
                    }
                }

                #endregion
                 
                return allowed;
            }
            finally
            {
                if (_Settings.Debug.Authentication)
                    _Logging.Info(logMsg + "[" + allowed.ToString() + "]: " + result.ToString());
            }
        }

        #endregion

        #region Private-Methods
         
        #endregion
    }
}
