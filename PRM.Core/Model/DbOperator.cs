﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using com.github.xiangyuecn.rsacsharp;
using PRM.Core.DB;
using PRM.Core.Protocol;
using PRM.Core.Protocol.Putty.SSH;
using PRM.Core.Protocol.RDP;
using Shawn.Utils;

namespace PRM.Core.Model
{
    public class DbOperator
    {
        private readonly IDb _db;
        public DbOperator(IDb db)
        {
            _db = db;
            var privateKeyPath = _db.Get_RSA_PrivateKeyPath();
            if (!string.IsNullOrWhiteSpace(privateKeyPath)
                && File.Exists(privateKeyPath))
            {
                _rsa = new RSA(File.ReadAllText(privateKeyPath), true);
            }
        }



        public bool IsDbEncrypted()
        {
            var rsaPrivateKeyPath = _db.Get_RSA_PrivateKeyPath();
            return !string.IsNullOrWhiteSpace(rsaPrivateKeyPath);
        }

        /// <summary>
        /// return:
        /// 0: encrypt ok
        /// 0: no encrypt
        /// -1: private key not existed
        /// -2: private key is not in correct format
        /// -3: private key not matched
        /// -4: db is not connected
        /// </summary>
        /// <returns></returns>
        public EnumDbStatus CheckDbRsaStatus()
        {
            if (_db?.IsConnected() != true)
                return EnumDbStatus.NotConnected;

            var rsaPrivateKeyPath = _db.Get_RSA_PrivateKeyPath();

            // NO RSA
            if (IsDbEncrypted() == false)
                return EnumDbStatus.OK;

            if (!File.Exists(rsaPrivateKeyPath))
                return EnumDbStatus.RsaPrivateKeyNotFound;

            try
            {
                var rsa = new RSA(File.ReadAllText(rsaPrivateKeyPath), true);
            }
            catch (Exception)
            {
                return EnumDbStatus.RsaPrivateKeyFormatError;
            }



            // make sure public key is PEM format key
            try
            {
                var rsaPublicKeyObj = new RSA(_db.Get_RSA_PublicKey(), true);
            }
            catch (Exception)
            {
                // TODO public key error, try SHA1 and pk matched?
                //// try to fix public key
                //if (rsa.Verify("SHA1", sha1, SystemConfig.AppName))
                //{
                //    this._db.Set_RSA_PublicKey(rsa.ToPEM_PKCS1(true));
                //    rsaPublicKeyObj = new RSA(File.ReadAllText(rsaPrivateKeyPath), true);
                //}
            }

            // check if RSA private key is matched public key?
            if (!VerifyRsaPrivateKey(rsaPrivateKeyPath))
            {
                return EnumDbStatus.RsaNotMatched;
            }
            return EnumDbStatus.OK;
        }

        private RSA _rsa = null;

        private string GetRsaPublicKey()
        {
            return _db.Get_RSA_PublicKey();
        }

        public string GetRsaPrivateKeyPath()
        {
            return _db.Get_RSA_PrivateKeyPath();
        }

        private void ClearRsaPrivateKey()
        {
            _db.Set_RSA_SHA1("");
            _db.Set_RSA_PublicKey("");
            _db.Set_RSA_PrivateKeyPath("");
        }

        public bool VerifyRsaPrivateKey(string privateKeyPath)
        {
            Debug.Assert(File.Exists(privateKeyPath));
            Debug.Assert(!string.IsNullOrWhiteSpace(privateKeyPath));

            if (IsDbEncrypted() == false)
                return false;

            // check if RSA private key is matched public key?
            try
            {
                var rsa = new RSA(File.ReadAllText(privateKeyPath), true);
                var sha1Tmp = rsa.Sign("SHA1", SystemConfig.AppName);
                var rsaPublicKeyObj = new RSA(_db.Get_RSA_PublicKey(), true);
                if (rsaPublicKeyObj?.Verify("SHA1", sha1Tmp, SystemConfig.AppName) != true)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// return
        /// 0: success
        /// -2: private key is not in correct format
        /// </summary>
        /// <param name="privateKeyPath"></param>
        /// <returns></returns>
        public int SetRsaPrivateKey(string privateKeyPath)
        {
            if (string.IsNullOrWhiteSpace(privateKeyPath))
            {
                ClearRsaPrivateKey();
                return 0;
            }

            Debug.Assert(File.Exists(privateKeyPath));

            RSA rsa = null;
            try
            {
                rsa = new RSA(File.ReadAllText(privateKeyPath), true);
            }
            catch (Exception e)
            {
                SimpleLogHelper.Debug(e);
                return -2;
            }

            _rsa = rsa;
            _db.Set_RSA_SHA1(rsa.Sign("SHA1", SystemConfig.AppName));
            _db.Set_RSA_PublicKey(rsa.ToPEM_PKCS1(true));
            _db.Set_RSA_PrivateKeyPath(privateKeyPath);
            return 0;
        }

        public void EncryptPwdIfItIsNotEncrypted(ProtocolServerBase server)
        {
            if (_rsa == null) return;
            if (server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortUserPwdBase)))
            {
                var s = (ProtocolServerWithAddrPortUserPwdBase)server;
                if (_rsa.DecodeOrNull(s.Password) == null)
                    s.Password = _rsa.Encode(s.Password);
            }
            switch (server)
            {
                case ProtocolServerSSH ssh when !string.IsNullOrWhiteSpace(ssh.PrivateKey):
                {
                    if (_rsa.DecodeOrNull(ssh.PrivateKey) == null)
                        ssh.PrivateKey = _rsa.Encode(ssh.PrivateKey);
                    break;
                }
                case ProtocolServerRDP rdp when !string.IsNullOrWhiteSpace(rdp.GatewayPassword):
                {
                    if (_rsa.DecodeOrNull(rdp.GatewayPassword) == null)
                        rdp.GatewayPassword = _rsa.Encode(rdp.GatewayPassword);
                    break;
                }
            }
        }

        public string DecryptOrReturnOriginalString(string originalString)
        {
            if (_rsa == null) return originalString;
            return _rsa.DecodeOrNull(originalString) ?? originalString;
        }

        public void DecryptPwdIfItIsEncrypted(ProtocolServerBase server)
        {
            if (_rsa == null) return;
            if (server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortUserPwdBase)))
            {
                var s = (ProtocolServerWithAddrPortUserPwdBase)server;
                s.Password = DecryptOrReturnOriginalString(s.Password);
            }
            switch (server)
            {
                case ProtocolServerSSH ssh when !string.IsNullOrWhiteSpace(ssh.PrivateKey):
                    Debug.Assert(_rsa.DecodeOrNull(ssh.PrivateKey) != null);
                    ssh.PrivateKey = DecryptOrReturnOriginalString(ssh.PrivateKey);
                    break;
                case ProtocolServerRDP rdp when !string.IsNullOrWhiteSpace(rdp.GatewayPassword):
                    Debug.Assert(_rsa.DecodeOrNull(rdp.GatewayPassword) != null);
                    rdp.GatewayPassword = DecryptOrReturnOriginalString(rdp.GatewayPassword);
                    break;
            }
        }

        public void EncryptInfo(ProtocolServerBase server)
        {
            if (_rsa == null) return;
            Debug.Assert(_rsa.DecodeOrNull(server.DispName) == null);
            server.DispName = _rsa.Encode(server.DispName);
            server.GroupName = _rsa.Encode(server.GroupName);

            if (server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortBase)))
            {
                var p = (ProtocolServerWithAddrPortUserPwdBase)server;
                if (!string.IsNullOrEmpty(p.UserName))
                    p.UserName = _rsa.Encode(p.UserName);
                if (!string.IsNullOrEmpty(p.Address))
                    p.Address = _rsa.Encode(p.Address);
            }
        }

        public void DecryptInfo(ProtocolServerBase server)
        {
            if (_rsa == null) return;
            Debug.Assert(_rsa.DecodeOrNull(server.DispName) != null);
            server.DispName = DecryptOrReturnOriginalString(server.DispName);
            server.GroupName = DecryptOrReturnOriginalString(server.GroupName);

            if (server.GetType().IsSubclassOf(typeof(ProtocolServerWithAddrPortBase)))
            {
                var p = (ProtocolServerWithAddrPortUserPwdBase)server;
                if (!string.IsNullOrEmpty(p.UserName))
                    p.UserName = DecryptOrReturnOriginalString(p.UserName) ?? p.UserName;
                if (!string.IsNullOrEmpty(p.Address))
                    p.Address = DecryptOrReturnOriginalString(p.Address) ?? p.Address;
            }
        }

        public void DbAddServer(ProtocolServerBase org)
        {
            EncryptPwdIfItIsNotEncrypted(org);
            var tmp = (ProtocolServerBase)org.Clone();
            tmp.SetNotifyPropertyChangedEnabled(false);
            EncryptInfo(tmp);
            _db.AddServer(tmp);
        }
        public void DbUpdateServer(ProtocolServerBase org)
        {
            EncryptPwdIfItIsNotEncrypted(org);
            var tmp = (ProtocolServerBase)org.Clone();
            tmp.SetNotifyPropertyChangedEnabled(false);
            EncryptInfo(tmp);
            _db.UpdateServer(tmp);
        }

        public bool DbDeleteServer(int id)
        {
            return _db.DeleteServer(id);
        }

        public List<ProtocolServerBase> GetServers()
        {
            return _db.GetServers();
        }
    }
}
