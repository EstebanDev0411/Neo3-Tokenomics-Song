using System;
using System.ComponentModel;
using System.Numerics;

// using Neo;
using Neo.SmartContract.Framework;
// using Neo.SmartContract.Framework.Attributes;
// using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;


namespace TokenSong
{
    [DisplayName("tomio111.SongTokenContract")]
    [ManifestExtra("Author", "tomio111")]
    [ManifestExtra("Email", "tobilokki99@gmail.com")]
    [ManifestExtra("Description", "Controls issuance of the Song token")]
    public class TokenSongContract : SmartContract
    {
        const string MAP_NAME = "SongTokenContract";
        public static BigInteger TotalSupply() => 1_000_000;
        public static string Symbol() => "Song";
        public static ulong Decimals() => 8;    

        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;
        
        private static StorageMap Balances => new StorageMap(Storage.CurrentContext, MAP_NAME);

        private static BigInteger Get(UInt160 key) => (BigInteger)Balances.Get(key);

        private static void Put(UInt160 key, BigInteger value) => Balances.Put(key, value);

        private static void Increase(UInt160 key, BigInteger value)
        {
            Put(key, Get(key) + value);
        }

        private static void Decrease(UInt160 key, BigInteger value)
        {
            var oldValue = Get(key);
            if(oldValue == value)
            {
                Balances.Delete(key);
            }
            else
            {
                Put(key, Get(key) - value);
            }
        }

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (!from.IsValid || !to.IsValid)
            {
                throw new Exception("The parameters from and to should be 20-byte addresses");
            }

            if (amount < 0) 
            {
                throw new Exception("The amount parameter must be greater than or equal to zero");
            }

            if (!from.Equals(Runtime.CallingScriptHash) && !Runtime.CheckWitness(from))
            {
                throw new Exception("No authorization.");
            }
            
            if (Get(from) < amount)
            {
                throw new Exception("Insufficient balance");
            }

            Reduce(from, amount);
            Increase(to, amount);
            OnTransfer(from, to, amount);

            if (ContractManagement.GetContract(to) != null)
            {
                Contract.Call(to, "onPayment", CallFlags.None, new object[] { from, amount, data });
            }
            
            return true;
        }

        public static BigInteger BalanceOf(UInt160 account)
        {
            return Get(account);
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                var tx = (Transaction) Runtime.ScriptContainer;
                var owner = (Neo.UInt160) tx.Sender;
                Increase(owner, InitialSupply);
                OnTransfer(null, owner, InitialSupply);
            }
        }



        // const byte Prefix_NumberStorage = 0x00;
        // const byte Prefix_ContractOwner = 0xFF;
        // private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        // [DisplayName("NumberChanged")]
        // public static event Action<UInt160, BigInteger> OnNumberChanged;

        // public static bool ChangeNumber(BigInteger positiveNumber)
        // {
        //     if (positiveNumber < 0)
        //     {
        //         throw new Exception("Only positive numbers are allowed.");
        //     }

        //     StorageMap contractStorage = new(Storage.CurrentContext, Prefix_NumberStorage);
        //     contractStorage.Put(Tx.Sender, positiveNumber);
        //     OnNumberChanged(Tx.Sender, positiveNumber);
        //     return true;
        // }

        // public static ByteString GetNumber()
        // {
        //     StorageMap contractStorage = new(Storage.CurrentContext, Prefix_NumberStorage);
        //     return contractStorage.Get(Tx.Sender);
        // }

        // [DisplayName("_deploy")]
        // public static void Deploy(object data, bool update)
        // {
        //     if (update) return;

        //     var key = new byte[] { Prefix_ContractOwner };
        //     Storage.Put(Storage.CurrentContext, key, Tx.Sender);
        // }
        
        // public static void Update(ByteString nefFile, string manifest)
        // {
        //     var key = new byte[] { Prefix_ContractOwner };
        //     var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);

        //     if (!contractOwner.Equals(Tx.Sender))
        //     {
        //         throw new Exception("Only the contract owner can update the contract");
        //     }

        //     ContractManagement.Update(nefFile, manifest, null);
        // }
    }
}
