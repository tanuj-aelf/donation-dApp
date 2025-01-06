using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using System.Linq;
using System.Collections.Generic;

namespace AElf.Contracts.DonationApp
{
    public class DonationApp : DonationDAppContainer.DonationDAppBase
    {
        public override BoolValue IsContractInitialized(Empty input)
        {
            return new BoolValue { Value = State.Initialized.Value };
        }

        public override Empty Initialize(Empty input)
        {
            Assert(State.Initialized.Value == false, "Already initialized.");
            State.Initialized.Value = true;
            State.Owner.Value = Context.Sender;
            return new Empty();
        }

        public override StringValue CreateCampaign(CampaignInput input)
        {
            Assert(State.Initialized.Value, "Contract not initialized.");
            var campaignId = HashHelper.ComputeFrom(input.Title).ToHex();
            var currentTime = Context.CurrentBlockTime.Seconds;
            var campaign = new Campaign
            {
                Title = input.Title,
                Description = input.Description,
                ImageUrl = input.ImageUrl,
                Type = input.Type,
                GoalAmount = input.GoalAmount,
                CurrentAmount = 0,
                Creator = Context.Sender,
                StartTime = currentTime,
                EndTime = currentTime + input.Duration,
                IsActive = true
            };
            
            State.Campaigns[campaignId] = campaign;

            // Update user's campaign list
            var userInfo = State.UserInfos[Context.Sender] ?? new UserInfo();
            userInfo.Campaigns.Add(campaignId);
            State.UserInfos[Context.Sender] = userInfo;

            // Fire campaign created event
            Context.Fire(new CampaignCreatedEvent
            {
                CampaignId = campaignId,
                Title = input.Title,
                Creator = Context.Sender,
                GoalAmount = input.GoalAmount
            });

            return new StringValue { Value = campaignId };
        }

        public override Empty Donate(DonationInput input)
        {
            Assert(State.Initialized.Value, "Contract not initialized.");
            var campaign = State.Campaigns[input.CampaignId];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.IsActive, "Campaign is not active.");
            Assert(Context.CurrentBlockTime.Seconds < campaign.EndTime, "Campaign has ended.");

            // Record donation
            var donation = new Donation
            {
                Donor = Context.Sender,
                Amount = input.Amount,
                Timestamp = Context.CurrentBlockTime.Seconds
            };
            campaign.Donators.Add(donation);
            campaign.CurrentAmount += input.Amount;

            // Update campaign
            State.Campaigns[input.CampaignId] = campaign;

            // Update user's donation history
            var userInfo = State.UserInfos[Context.Sender] ?? new UserInfo();
            if (!userInfo.DonatedCampaigns.Contains(input.CampaignId))
            {
                userInfo.DonatedCampaigns.Add(input.CampaignId);
            }
            State.UserInfos[Context.Sender] = userInfo;

            // Fire donation made event
            Context.Fire(new DonationMadeEvent
            {
                CampaignId = input.CampaignId,
                Donor = Context.Sender,
                Amount = input.Amount
            });

            return new Empty();
        }

        public override CampaignList GetCampaignsData(Empty input)
        {
            var campaigns = new List<Campaign>();
            var processedCampaignIds = new HashSet<string>();

            // Get campaigns from owner's list first
            var ownerInfo = State.UserInfos[State.Owner.Value];
            if (ownerInfo != null)
            {
                foreach (var campaignId in ownerInfo.Campaigns)
                {
                    var campaign = State.Campaigns[campaignId];
                    if (campaign != null)
                    {
                        campaigns.Add(campaign);
                        processedCampaignIds.Add(campaignId);
                    }
                }
            }

            // Get campaigns from sender's list
            var senderInfo = State.UserInfos[Context.Sender];
            if (senderInfo != null)
            {
                foreach (var campaignId in senderInfo.Campaigns)
                {
                    if (!processedCampaignIds.Contains(campaignId))
                    {
                        var campaign = State.Campaigns[campaignId];
                        if (campaign != null)
                        {
                            campaigns.Add(campaign);
                            processedCampaignIds.Add(campaignId);
                        }
                    }
                }
            }

            return new CampaignList { Value = { campaigns } };
        }

        public override Campaign GetCampaign(StringValue input)
        {
            var campaign = State.Campaigns[input.Value];
            Assert(campaign != null, "Campaign does not exist.");
            return campaign;
        }

        public override CampaignList GetUsersCampaigns(Address input)
        {
            var userInfo = State.UserInfos[input];
            if (userInfo == null || userInfo.Campaigns.Count == 0)
            {
                return new CampaignList();
            }

            var campaigns = new List<Campaign>();
            foreach (var id in userInfo.Campaigns)
            {
                var campaign = State.Campaigns[id];
                if (campaign != null)
                {
                    campaigns.Add(campaign);
                }
            }

            return new CampaignList { Value = { campaigns } };
        }

        public override Empty EditCampaign(EditCampaignInput input)
        {
            var campaign = State.Campaigns[input.CampaignId];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.Creator == Context.Sender, "Only the creator can edit the campaign.");

            if (!string.IsNullOrEmpty(input.NewTitle))
                campaign.Title = input.NewTitle;
            if (!string.IsNullOrEmpty(input.NewDescription))
                campaign.Description = input.NewDescription;
            if (!string.IsNullOrEmpty(input.NewImageUrl))
                campaign.ImageUrl = input.NewImageUrl;
            if (!string.IsNullOrEmpty(input.NewType))
                campaign.Type = input.NewType;
            if (input.NewGoalAmount != 0)
                campaign.GoalAmount = input.NewGoalAmount;
            
            campaign.IsActive = input.NewIsActive;

            State.Campaigns[input.CampaignId] = campaign;
            return new Empty();
        }

        public override Empty DeleteCampaign(StringValue input)
        {
            var campaign = State.Campaigns[input.Value];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.Creator == Context.Sender, "Only the creator can delete the campaign.");

            // Remove campaign
            State.Campaigns.Remove(input.Value);

            // Update user info
            var userInfo = State.UserInfos[Context.Sender];
            if (userInfo != null)
            {
                userInfo.Campaigns.Remove(input.Value);
                State.UserInfos[Context.Sender] = userInfo;
            }

            return new Empty();
        }

        public override Empty SendReward(RewardInput input)
        {
            var campaign = State.Campaigns[input.CampaignId];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.Creator == Context.Sender, "Only the creator can send rewards.");
            
            var reward = new Donation
            {
                Donor = Context.Sender,
                Amount = input.Amount,
                Timestamp = Context.CurrentBlockTime.Seconds
            };

            var rewards = State.CampaignRewards[input.CampaignId] ?? new List<Donation>();
            rewards.Add(reward);
            State.CampaignRewards[input.CampaignId] = rewards;

            // Fire reward sent event
            Context.Fire(new RewardSentEvent
            {
                CampaignId = input.CampaignId,
                Recipient = input.Recipient,
                Amount = input.Amount,
                Message = input.Message
            });

            return new Empty();
        }

        public override UserDetails GetUserDetails(Address input)
        {
            var userInfo = State.UserInfos[input] ?? new UserInfo();
            
            var campaigns = new List<Campaign>();
            foreach (var id in userInfo.Campaigns)
            {
                var campaign = State.Campaigns[id];
                if (campaign != null)
                {
                    campaigns.Add(campaign);
                }
            }

            var donatedCampaigns = new List<Campaign>();
            foreach (var id in userInfo.DonatedCampaigns)
            {
                var campaign = State.Campaigns[id];
                if (campaign != null)
                {
                    donatedCampaigns.Add(campaign);
                }
            }

            return new UserDetails
            {
                WalletAddress = input,
                Campaigns = { campaigns },
                DonatedCampaigns = { donatedCampaigns },
                TotalRaisedAmount = userInfo.TotalRaisedAmount
            };
        }
    }
} 