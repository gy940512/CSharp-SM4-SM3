﻿using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace net45.Utility
{
    public class Cipher
    {
        private int ct;
        private ECPoint p2;
        private SM3Digest sm3keybase;
        private SM3Digest sm3c3;
        private byte[] key;
        private byte keyOff;

        public Cipher()
        {
            ct = 1;
            key = new byte[32];
            keyOff = 0;
        }

        public static byte[] byteConvert32Bytes(BigInteger n)
        {
            byte[] tmpd = null;
            if (n == null)
            {
                return null;
            }

            if (n.ToByteArray().Length == 33)
            {
                tmpd = new byte[32];
                Array.Copy(n.ToByteArray(), 1, tmpd, 0, 32);
            }
            else if (n.ToByteArray().Length == 32)
            {
                tmpd = n.ToByteArray();
            }
            else
            {
                tmpd = new byte[32];
                for (var i = 0; i < 32 - n.ToByteArray().Length; i++)
                {
                    tmpd[i] = 0;
                }
                Array.Copy(n.ToByteArray(), 0, tmpd, 32 - n.ToByteArray().Length, n.ToByteArray().Length);
            }
            return tmpd;
        }

        private void Reset()
        {
            sm3keybase = new SM3Digest();
            sm3c3 = new SM3Digest();

            var p = byteConvert32Bytes(p2.Normalize().XCoord.ToBigInteger());
            sm3keybase.BlockUpdate(p, 0, p.Length);
            sm3c3.BlockUpdate(p, 0, p.Length);

            p = byteConvert32Bytes(p2.Normalize().YCoord.ToBigInteger());
            sm3keybase.BlockUpdate(p, 0, p.Length);
            ct = 1;
            NextKey();
        }

        private void NextKey()
        {
            var sm3keycur = new SM3Digest(sm3keybase);
            sm3keycur.Update((byte)(ct >> 24 & 0xff));
            sm3keycur.Update((byte)(ct >> 16 & 0xff));
            sm3keycur.Update((byte)(ct >> 8 & 0xff));
            sm3keycur.Update((byte)(ct & 0xff));
            sm3keycur.DoFinal(key, 0);
            keyOff = 0;
            ct++;
        }

        public ECPoint Init_enc(SM2 sm2, ECPoint userKey)
        {
            var key = sm2.ecc_key_pair_generator.GenerateKeyPair();
            var ecpriv = (ECPrivateKeyParameters)key.Private;
            var ecpub = (ECPublicKeyParameters)key.Public;
            var k = ecpriv.D;
            var c1 = ecpub.Q;
            p2 = userKey.Multiply(k);
            Reset();
            return c1;
        }

        public void Encrypt(byte[] data)
        {
            sm3c3.BlockUpdate(data, 0, data.Length);
            for (var i = 0; i < data.Length; i++)
            {
                if (keyOff == key.Length)
                {
                    NextKey();
                }
                data[i] ^= key[keyOff++];
            }
        }

        public void Init_dec(BigInteger userD, ECPoint c1)
        {
            p2 = c1.Multiply(userD);
            Reset();
        }

        public void Decrypt(byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (keyOff == key.Length)
                {
                    NextKey();
                }
                data[i] ^= key[keyOff++];
            }

            sm3c3.BlockUpdate(data, 0, data.Length);
        }

        public void Dofinal(byte[] c3)
        {
            var p = byteConvert32Bytes(p2.Normalize().YCoord.ToBigInteger());
            sm3c3.BlockUpdate(p, 0, p.Length);
            sm3c3.DoFinal(c3, 0);
            Reset();
        }
    }
}
