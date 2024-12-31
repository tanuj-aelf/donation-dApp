using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DonationDApp
{
    public class DonationDAppTests : TestBase
    {
        [Fact]
        public async Task Initialize_ShouldSetOwner()
        {
            // Arrange
            var input = new Empty();

            // Act
            await DonationDAppStub.Initialize.SendAsync(input);

            // Assert
            var owner = await DonationDAppStub.GetOwner.CallAsync(new Empty());
            owner.ShouldBe(DefaultKeyPair);
        }

        [Fact]
        public async Task CreateCampaign_ShouldCreateNewCampaign()
        {
            // Arrange
            var input = new CampaignInput
            {
                Title = "Test Campaign",
                Description = "This is a test campaign.",
                ImageUrl = "http://example.com/image.png",
                Type = "Education",
                TargetFund = 1000,
                EndDate = 1735616381 // Example timestamp
            };

            // Act
            var campaignId = await DonationDAppStub.CreateCampaign.SendAsync(input);

            // Assert
            var campaign = await DonationDAppStub.GetCampaign.CallAsync(campaignId);
            campaign.Title.ShouldBe(input.Title);
            campaign.Description.ShouldBe(input.Description);
        }

        [Fact]
        public async Task Donate_ShouldUpdateRaisedFund()
        {
            // Arrange
            var campaignInput = new CampaignInput
            {
                Title = "Test Campaign",
                Description = "This is a test campaign.",
                ImageUrl = "http://example.com/image.png",
                Type = "Education",
                TargetFund = 1000,
                EndDate = 1735616381 // Example timestamp
            };
            var campaignId = await DonationDAppStub.CreateCampaign.SendAsync(campaignInput);

            var donationInput = new DonationInput
            {
                CampaignId = campaignId.Value,
                Amount = 100
            };

            // Act
            await DonationDAppStub.Donate.SendAsync(donationInput);

            // Assert
            var campaign = await DonationDAppStub.GetCampaign.CallAsync(campaignId);
            campaign.RaisedFund.ShouldBe(100);
        }

        [Fact]
        public async Task EditCampaign_ShouldUpdateCampaignDetails()
        {
            // Arrange
            var campaignInput = new CampaignInput
            {
                Title = "Test Campaign",
                Description = "This is a test campaign.",
                ImageUrl = "http://example.com/image.png",
                Type = "Education",
                TargetFund = 1000,
                EndDate = 1735616381 // Example timestamp
            };
            var campaignId = await DonationDAppStub.CreateCampaign.SendAsync(campaignInput);

            var editInput = new EditCampaignInput
            {
                Id = campaignId.Value,
                Title = "Updated Campaign Title",
                Description = "Updated description."
            };

            // Act
            await DonationDAppStub.EditCampaign.SendAsync(editInput);

            // Assert
            var updatedCampaign = await DonationDAppStub.GetCampaign.CallAsync(campaignId);
            updatedCampaign.Title.ShouldBe("Updated Campaign Title");
            updatedCampaign.Description.ShouldBe("Updated description.");
        }

        [Fact]
        public async Task DeleteCampaign_ShouldRemoveCampaign()
        {
            // Arrange
            var campaignInput = new CampaignInput
            {
                Title = "Test Campaign",
                Description = "This is a test campaign.",
                ImageUrl = "http://example.com/image.png",
                Type = "Education",
                TargetFund = 1000,
                EndDate = 1735616381 // Example timestamp
            };
            var campaignId = await DonationDAppStub.CreateCampaign.SendAsync(campaignInput);

            // Act
            await DonationDAppStub.DeleteCampaign.SendAsync(new StringValue { Value = campaignId.Value });

            // Assert
            var deletedCampaign = await DonationDAppStub.GetCampaign.CallAsync(campaignId);
            deletedCampaign.Description.ShouldBe("Campaign not found.");
        }
    }
} 