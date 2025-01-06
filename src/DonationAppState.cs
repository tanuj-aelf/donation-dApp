using AElf.Sdk.CSharp.State;
using AElf.Types;
using System.Collections.Generic;

namespace AElf.Contracts.DonationApp
{
    public class UserInfo
    {
        public List<string> Campaigns { get; set; } = new List<string>();
        public List<string> DonatedCampaigns { get; set; } = new List<string>();
        public long TotalRaisedAmount { get; set; }
    }

    public partial class DonationAppState : ContractState
    {
        public BoolState Initialized { get; set; }
        public SingletonState<Address> Owner { get; set; }
        
        // Maps campaign ID to Campaign
        public MappedState<string, Campaign> Campaigns { get; set; }
        
        // Maps user address to their info (campaigns and donations)
        public MappedState<Address, UserInfo> UserInfos { get; set; }
        
        // Maps campaign ID to list of rewards sent
        public MappedState<string, List<Donation>> CampaignRewards { get; set; }
    }
} 