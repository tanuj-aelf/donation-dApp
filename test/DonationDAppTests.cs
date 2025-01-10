using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DonationApp
{
    public class DonationDAppTests : DonationDAppTestBase
    {
        [Fact]
        public async Task Initialize_Test()
        {
            // Arrange & Act
            var result = await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            // Assert
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var isInitialized = await DonationContract.IsContractInitialized.CallAsync(new Empty());
            isInitialized.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task CreateCampaign_Success()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var input = new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(),
                EndTime = GetTimestamp(30)  // 30 days from now
            };

            // Act
            var result = await DonationContract.CreateCampaign.SendAsync(input);

            // Assert
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var campaignId = result.Output;
            campaignId.ShouldNotBeNull();

            var campaign = await DonationContract.GetCampaign.CallAsync(new StringValue { Value = campaignId.Value });
            campaign.Title.ShouldBe(input.Title);
            campaign.Description.ShouldBe(input.Description);
            campaign.TargetAmount.ShouldBe(input.TargetAmount);
            campaign.StartTime.ShouldBe(input.StartTime);
            campaign.EndTime.ShouldBe(input.EndTime);
            campaign.Owner.ShouldBe(DefaultAddress);
        }
    }
} 