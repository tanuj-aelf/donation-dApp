using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.DonationDApp
{
    public class DonationApp : DonationAppContainer.DonationAppBase
    {
        // Initializes the contract
        public override Empty Initialize(Empty input)
        {
            Assert(State.Initialized.Value == false, "Already initialized.");
            State.Initialized.Value = true;
            State.Owner.Value = Context.Sender;
            return new Empty();
        }

        // Create a new campaign
        public override StringValue CreateCampaign(CampaignInput input)
        {
            Assert(State.Initialized.Value, "Contract not initialized.");
            var campaignId = Hash.FromString(input.Title).ToHex();
            var campaign = new Campaign
            {
                Title = input.Title,
                Description = input.Description,
                GoalAmount = input.GoalAmount,
                CurrentAmount = 0,
                Creator = Context.Sender,
                StartTime = Context.CurrentBlockTime,
                EndTime = Context.CurrentBlockTime.AddSeconds(input.Duration)
            };
            State.Campaigns[campaignId] = campaign;
            return new StringValue { Value = campaignId };
        }

        public override Empty Donate(DonationInput input)
        {
            Assert(State.Initialized.Value, "Contract not initialized.");
            var campaign = State.Campaigns[input.CampaignId];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(Context.CurrentBlockTime < campaign.EndTime, "Campaign has ended.");

            // Logic to process donation
            campaign.CurrentAmount += input.Amount;
            // Additional logic to handle the transfer of funds can be added here

            State.Campaigns[input.CampaignId] = campaign;
            return new Empty();
        }

        public override Campaign GetCampaign(StringValue input)
        {
            var campaign = State.Campaigns[input.Value];
            Assert(campaign != null, "Campaign does not exist.");
            return campaign;
        }

        public override Empty EditCampaign(EditCampaignInput input)
        {
            var campaign = State.Campaigns[input.CampaignId];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.Creator == Context.Sender, "Only the creator can edit the campaign.");

            // Logic to edit campaign details
            campaign.Title = input.NewTitle ?? campaign.Title;
            campaign.Description = input.NewDescription ?? campaign.Description;
            campaign.GoalAmount = input.NewGoalAmount ?? campaign.GoalAmount;

            State.Campaigns[input.CampaignId] = campaign;
            return new Empty();
        }

        public override Empty DeleteCampaign(StringValue input)
        {
            var campaign = State.Campaigns[input.Value];
            Assert(campaign != null, "Campaign does not exist.");
            Assert(campaign.Creator == Context.Sender, "Only the creator can delete the campaign.");

            // Logic to delete a campaign
            State.Campaigns.Remove(input.Value);
            return new Empty();
        }
    }
} 