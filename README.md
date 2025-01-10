# Donation DApp

A decentralized donation application built on AElf blockchain that allows users to create and manage fundraising campaigns. Users can create campaigns with specific goals and time frames, and others can donate to these campaigns using ELF tokens.

## Features

- Create donation campaigns with title, description, target amount, and duration
- Donate ELF tokens to active campaigns
- View campaign details including current amount raised and donor list
- Automatic campaign state management based on start and end times
- Token approval and transfer handling

## Project Structure

```
donation/
├── src/                    # Smart contract source code
│   ├── Donation.csproj     # Contract project file
│   ├── DonationApp.cs      # Main contract implementation
│   └── Protobuf/          # Protocol buffer definitions
├── test/                   # Unit tests
│   ├── DonationDAppTests.csproj
│   └── DonationDAppTests.cs
```

## Prerequisites

- .NET SDK 8.0 or later
- AElf Contract Development Kit
- Node.js and npm (for deploying to testnet/mainnet)
- AElf CLI tools (`npm install -g aelf-command`)

## Build Instructions

Build the contract:
```bash
cd donation/src
dotnet build
```

## Deployment

### Environment Setup

1. Set your environment variables:
```bash
export WALLET_ADDRESS="your_wallet_address"
export WALLET_PASSWORD="your_wallet_password"
export CONTRACT_PATH="./src/bin/Debug/net8.0/Donation.dll"
```

### Local Development Node

1. Start your local AElf node
2. Deploy the contract:
```bash
aelf-deploy -a $WALLET_ADDRESS -p $WALLET_PASSWORD -c $CONTRACT_PATH -e http://127.0.0.1:8000
```

### Testnet Deployment

1. Deploy to testnet:
```bash
aelf-deploy -a $WALLET_ADDRESS -p $WALLET_PASSWORD -c $CONTRACT_PATH -e https://tdvw-test-node.aelf.io/
```

## Contract Interaction

### Initialize Contract
```bash
aelf-command send $CONTRACT_ADDRESS -a $WALLET_ADDRESS -p $WALLET_PASSWORD -e https://tdvw-test-node.aelf.io Initialize
```

### Create Campaign
```bash
aelf-command send $CONTRACT_ADDRESS -a $WALLET_ADDRESS -p $WALLET_PASSWORD -e https://tdvw-test-node.aelf.io CreateCampaign \
-j '{"title":"Campaign Title","description":"Description","targetAmount":1000000000,"startTime":"2024-01-10","endTime":"2024-02-10"}'
```

### Donate to Campaign
```bash
# First approve token spending
aelf-command send $TOKEN_CONTRACT_ADDRESS -a $WALLET_ADDRESS -p $WALLET_PASSWORD -e https://tdvw-test-node.aelf.io Approve \
-j '{"spender":"'$CONTRACT_ADDRESS'","symbol":"ELF","amount":1000000000}'

# Then donate
aelf-command send $CONTRACT_ADDRESS -a $WALLET_ADDRESS -p $WALLET_PASSWORD -e https://tdvw-test-node.aelf.io Donate \
-j '{"campaignId":"'$CAMPAIGN_ID'","amount":1000000000}'
```

### View Campaign Details
```bash
aelf-command call $CONTRACT_ADDRESS -a $WALLET_ADDRESS -e https://tdvw-test-node.aelf.io GetCampaign \
-j '{"value":"'$CAMPAIGN_ID'"}'
```

## Testing

The project includes comprehensive unit tests covering:
- Contract initialization
- Campaign creation and validation
- Donation functionality
- Campaign state management
- Query operations

Run tests with coverage:
```bash
cd test
dotnet test
```

## License

This project is licensed under the MIT License. 