using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.DonationDApp
{
    public partial class DonationAppState : ContractState
    {
        public BoolState Initialized { get; set; }
        public SingletonState<Address> Owner { get; set; }
        public MappedState<string, Campaign> Campaigns { get; set; } // Maps campaign ID to Campaign
    }
} 