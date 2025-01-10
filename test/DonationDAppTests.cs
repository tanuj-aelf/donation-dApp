using System;
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
        public async Task Initialize_Success()
        {
            // Arrange & Act
            var result = await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            // Assert
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var isInitialized = await DonationContract.IsContractInitialized.CallAsync(new Empty());
            isInitialized.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task Initialize_AlreadyInitialized_ShouldFail()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.Initialize.SendAsync(new InitializeInput()));
            exception.Message.ShouldContain("Contract already initialized");
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
            campaign.CurrentAmount.ShouldBe(0);
        }

        [Fact]
        public async Task CreateCampaign_InvalidDates_ShouldFail()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var input = new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(30),  // Start time after end time
                EndTime = GetTimestamp(1)
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.CreateCampaign.SendAsync(input));
            exception.Message.ShouldContain("End time must be after start time");
        }

        [Fact]
        public async Task CreateCampaign_NotInitialized_ShouldFail()
        {
            // Arrange
            var input = new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(),
                EndTime = GetTimestamp(30)
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.CreateCampaign.SendAsync(input));
            exception.Message.ShouldContain("Contract not initialized");
        }

        [Fact]
        public async Task Donate_Success()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var createResult = await DonationContract.CreateCampaign.SendAsync(new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(),
                EndTime = GetTimestamp(30)
            });
            var campaignId = createResult.Output.Value;

            // Approve token spending
            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);

            // Act
            var result = await DonationContract.Donate.SendAsync(new DonateInput
            {
                CampaignId = campaignId,
                Amount = DefaultDonationAmount
            });

            // Assert
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var campaign = await DonationContract.GetCampaign.CallAsync(new StringValue { Value = campaignId });
            campaign.CurrentAmount.ShouldBe(DefaultDonationAmount);

            var donatorList = await DonationContract.GetDonatorList.CallAsync(new StringValue { Value = campaignId });
            donatorList.Value.Count.ShouldBe(1);
            donatorList.Value[0].Donor.ShouldBe(DefaultAddress);
            donatorList.Value[0].Amount.ShouldBe(DefaultDonationAmount);
        }

        [Fact]
        public async Task Donate_MultipleDonations_Success()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var createResult = await DonationContract.CreateCampaign.SendAsync(new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(),
                EndTime = GetTimestamp(30)
            });
            var campaignId = createResult.Output.Value;

            // First donation
            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);
            await DonationContract.Donate.SendAsync(new DonateInput
            {
                CampaignId = campaignId,
                Amount = DefaultDonationAmount
            });

            // Second donation
            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);
            await DonationContract.Donate.SendAsync(new DonateInput
            {
                CampaignId = campaignId,
                Amount = DefaultDonationAmount
            });

            // Assert
            var campaign = await DonationContract.GetCampaign.CallAsync(new StringValue { Value = campaignId });
            campaign.CurrentAmount.ShouldBe(DefaultDonationAmount * 2);

            var donatorList = await DonationContract.GetDonatorList.CallAsync(new StringValue { Value = campaignId });
            donatorList.Value.Count.ShouldBe(1);  // Same donor
            donatorList.Value[0].Amount.ShouldBe(DefaultDonationAmount * 2);
        }

        [Fact]
        public async Task Donate_CampaignNotFound_ShouldFail()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.Donate.SendAsync(new DonateInput
                {
                    CampaignId = "nonexistent",
                    Amount = DefaultDonationAmount
                }));
            exception.Message.ShouldContain("Campaign not found");
        }

        [Fact]
        public async Task Donate_CampaignEnded_ShouldFail()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var createResult = await DonationContract.CreateCampaign.SendAsync(new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(-30),  // Started 30 days ago
                EndTime = GetTimestamp(-1)      // Ended yesterday
            });
            var campaignId = createResult.Output.Value;

            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.Donate.SendAsync(new DonateInput
                {
                    CampaignId = campaignId,
                    Amount = DefaultDonationAmount
                }));
            exception.Message.ShouldContain("Campaign has ended");
        }

        [Fact]
        public async Task Donate_CampaignNotStarted_ShouldFail()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            var createResult = await DonationContract.CreateCampaign.SendAsync(new CreateCampaignInput
            {
                Title = "Test Campaign",
                Description = "Test Description",
                TargetAmount = 100_00000000,
                StartTime = GetTimestamp(1),    // Starts tomorrow
                EndTime = GetTimestamp(30)
            });
            var campaignId = createResult.Output.Value;

            await ApproveTokenAsync(DonationContractAddress, DefaultDonationAmount);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                DonationContract.Donate.SendAsync(new DonateInput
                {
                    CampaignId = campaignId,
                    Amount = DefaultDonationAmount
                }));
            exception.Message.ShouldContain("Campaign has not started");
        }

        [Fact]
        public async Task GetCampaignsData_Success()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());
            
            // Create multiple campaigns
            for (int i = 0; i < 3; i++)
            {
                await DonationContract.CreateCampaign.SendAsync(new CreateCampaignInput
                {
                    Title = $"Campaign {i}",
                    Description = $"Description {i}",
                    TargetAmount = 100_00000000,
                    StartTime = GetTimestamp(),
                    EndTime = GetTimestamp(30)
                });
            }

            // Act
            var result = await DonationContract.GetCampaignsData.CallAsync(new Empty());

            // Assert
            result.Value.Count.ShouldBe(3);
            for (int i = 0; i < 3; i++)
            {
                var campaign = result.Value[i];
                campaign.Title.ShouldBe($"Campaign {i}");
                campaign.Description.ShouldBe($"Description {i}");
                campaign.TargetAmount.ShouldBe(100_00000000);
                campaign.CurrentAmount.ShouldBe(0);
                campaign.Owner.ShouldBe(DefaultAddress);
            }
        }

        [Fact]
        public async Task GetCampaign_NotFound_ShouldReturnNull()
        {
            // Arrange
            await DonationContract.Initialize.SendAsync(new InitializeInput());

            // Act
            var campaign = await DonationContract.GetCampaign.CallAsync(new StringValue { Value = "nonexistent" });

            // Assert
            campaign.ShouldBeNull();
        }
    }
} 