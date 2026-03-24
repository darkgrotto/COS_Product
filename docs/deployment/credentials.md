# Credentials and Secrets Setup Guide

This guide covers everything you need before running the first-run wizard. Each section is specific to one deployment type.

The wizard collects all configuration interactively and provisions infrastructure on your behalf. For cloud deployments, you only need to install the required CLI tools and log in before starting.

---

## All Deployment Types

Regardless of deployment type, the wizard creates the following accounts during setup. No external setup is required for these - you choose the values yourself.

| Account | Wizard step | Notes |
|---------|------------|-------|
| Database admin | Step 8 | PostgreSQL superuser for the instance database. Not an application account. |
| Product admin | Step 9 | Built-in local admin for CountOrSell. Cannot be removed, disabled, or converted to OAuth. |
| General user | Step 10 | One general user account. Additional users are created post-setup. |

**Password requirement:** All three accounts require a minimum of 15 characters. Choose your passwords before starting the wizard.

---

## Docker Compose

### What the wizard needs

| Item | Where it comes from |
|------|-------------------|
| Docker registry | Default: `ghcr.io/darkgrotto/countorsell` (official registry, no authentication required) |
| Three account passwords | You choose (15+ characters each) |
| Backup destination | Local path or cloud storage credentials (see below) |

### Docker registry

The official CountOrSell image is published at `ghcr.io/darkgrotto/countorsell`. This is a public registry - no authentication is required to pull from it. The wizard defaults to `ghcr.io/darkgrotto/countorsell`. Press Enter to accept the default unless you are hosting the image in your own private registry.

### Backup destination (Step 11)

The wizard requires one backup destination. Choose one:

**Local file export:** Enter a local directory path (default: `./backups`). No credentials required. The backup container writes files to this path on the Docker host.

**Azure Blob Storage:** You need an Azure Blob Storage connection string. Obtain this from the Azure portal:
1. Open your storage account.
2. Go to **Security + networking > Access keys**.
3. Copy the **Connection string** for key1 or key2.

The connection string format is:
```
DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net
```

**AWS S3:** You need a bucket name and AWS region. Create the S3 bucket in advance and provide its name and region to the wizard.

**GCP Storage:** You need a GCS bucket name. Create the GCS bucket in advance and provide its name to the wizard. The application must have credentials to write to that bucket at runtime.

### Post-setup credentials

These are not required during the wizard but are needed later:

| Credential | Where to get it | Configured in |
|-----------|----------------|---------------|
| OAuth client credentials | See [OAuth Providers](#oauth-providers) below | Admin panel post-setup |
| TCGPlayer API key | TCGPlayer developer portal | Admin panel post-setup |

---

## Azure

### Overview of what you need

| Item | Notes |
|------|-------|
| Azure CLI (`az`) installed | Install instructions below |
| Terraform installed | Install instructions below |
| Active Azure subscription | Log in with `az login` before running the wizard |
| Application resource group name | You choose; Terraform creates it |
| Azure location | You choose (e.g. `eastus`) |
| Terraform state resource group name | You choose; the wizard creates it |
| Terraform state storage account name | You choose; globally unique, 3-24 lowercase alphanumeric; the wizard creates it |

The wizard detects your active subscription and tenant automatically from your login. No credentials need to be entered or set as environment variables.

### Step 1 - Install prerequisites

**Azure CLI:**
```
# macOS
brew install azure-cli

# Windows (winget)
winget install Microsoft.AzureCLI

# Linux - see https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux
```

**Terraform:**
```
# macOS
brew tap hashicorp/tap && brew install hashicorp/tap/terraform

# Other platforms: https://developer.hashicorp.com/terraform/install
```

### Step 2 - Log in to Azure

```
az login
```

This opens a browser window for authentication. When complete, `az account show` should return your subscription details. The wizard checks this automatically and will prompt you if you are not logged in.

### Step 3 - Choose your resource names

Decide on names before running the wizard:

- **Application resource group name:** Any valid Azure resource group name (e.g. `countorsell-rg`). Terraform creates this.
- **Azure location:** The region for all resources (e.g. `eastus`, `westeurope`).
- **Terraform state resource group name:** A separate resource group to hold Terraform state storage (e.g. `countorsell-tfstate-rg`). The wizard creates this.
- **Terraform state storage account name:** Must be globally unique across all Azure accounts, 3-24 lowercase alphanumeric characters (e.g. `myorgcountorselltf`). The wizard creates this.

### Step 4 - Add GitHub Actions secrets (optional)

If you are using the included GitHub Actions workflow (`azure.yml`) to manage infrastructure, add these secrets to your GitHub repository (**Settings > Secrets and variables > Actions**):

| Secret name | Value |
|------------|-------|
| `AZURE_CLIENT_ID` | Service principal app ID (create one with `az ad sp create-for-rbac`) |
| `AZURE_CLIENT_SECRET` | Service principal password |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID (`az account show --query id -o tsv`) |
| `AZURE_TENANT_ID` | Your tenant ID (`az account show --query tenantId -o tsv`) |
| `TF_STATE_RESOURCE_GROUP` | The Terraform state resource group name you chose |
| `TF_STATE_STORAGE_ACCOUNT` | The Terraform state storage account name you chose |

The GitHub Actions workflow uses a service principal for unattended runs. The wizard uses your interactive login and does not require a service principal.

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| (auto-detected) Subscription ID | From `az account show` |
| (auto-detected) Tenant ID | From `az account show` |
| Application resource group name | Name you choose; Terraform creates it |
| Azure location | e.g. `eastus`, `westeurope` |
| Terraform state resource group name | Name you choose; wizard creates it |
| Terraform state storage account name | Name you choose; wizard creates it |

---

## AWS

### Overview of what you need

| Item | Notes |
|------|-------|
| AWS CLI (`aws`) installed | Install instructions below |
| Terraform installed | Install instructions below |
| AWS credentials configured | Any standard AWS credential method (see below) |
| AWS region | You choose |
| Terraform state S3 bucket name | You choose; globally unique; the wizard creates it |

The wizard validates your credentials automatically and will prompt you if they are not working. No access key or secret needs to be entered into the wizard directly.

### Step 1 - Install prerequisites

**AWS CLI:**
```
# macOS
brew install awscli

# Windows (winget)
winget install Amazon.AWSCLI

# Linux: https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html
```

**Terraform:**
```
# macOS
brew tap hashicorp/tap && brew install hashicorp/tap/terraform

# Other platforms: https://developer.hashicorp.com/terraform/install
```

### Step 2 - Configure AWS credentials

The wizard uses whatever credentials are active in your shell. Any standard AWS credential method works:

**Option A: Interactive configuration (stores credentials in `~/.aws/credentials`)**
```
aws configure
```
Enter your access key ID, secret access key, and default region when prompted.

**Option B: Environment variables**
```
export AWS_ACCESS_KEY_ID=<your-access-key-id>
export AWS_SECRET_ACCESS_KEY=<your-secret-access-key>
export AWS_DEFAULT_REGION=us-east-1
```

**Option C: IAM role** - If running on an EC2 instance or other AWS service with an attached IAM role, credentials are available automatically.

The credentials must have sufficient permissions to create App Runner services, RDS instances, Secrets Manager secrets, S3 buckets, IAM roles, and VPC resources.

After configuring, verify credentials work:
```
aws sts get-caller-identity
```

### Step 3 - Choose your resource names

- **AWS region:** The region for all resources (e.g. `us-east-1`, `eu-west-1`).
- **Terraform state S3 bucket name:** Must be globally unique across all AWS accounts (e.g. `myorg-countorsell-tfstate`). The wizard creates this bucket.

### Step 4 - Add GitHub Actions secrets (optional)

If you are using the included GitHub Actions workflow (`aws.yml`) to manage infrastructure, add these secrets to your GitHub repository:

| Secret name | Value |
|------------|-------|
| `AWS_ACCESS_KEY_ID` | Access key ID for the IAM user used by CI |
| `AWS_SECRET_ACCESS_KEY` | Secret access key for the IAM user used by CI |
| `AWS_DEFAULT_REGION` | Your chosen AWS region (e.g. `us-east-1`) |
| `TF_STATE_BUCKET` | The S3 bucket name you chose for Terraform state |

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| (auto-detected) AWS region | From `aws configure get region`, or you enter it |
| AWS region | e.g. `us-east-1`, `eu-west-1` |
| Terraform state S3 bucket name | Name you choose; wizard creates it |

---

## GCP

### Overview of what you need

| Item | Notes |
|------|-------|
| Google Cloud CLI (`gcloud`) installed | Install instructions below |
| Terraform installed | Install instructions below |
| Active gcloud login | `gcloud auth login` before running the wizard |
| Application default credentials | `gcloud auth application-default login` before running the wizard |
| GCP project ID | From your existing GCP project |
| GCP region | You choose |
| Terraform state GCS bucket name | You choose; globally unique; the wizard creates it |

The wizard enables required GCP APIs and creates the Terraform state bucket automatically. No service account key file is required for the wizard.

### Step 1 - Install prerequisites

**Google Cloud CLI:**
```
# macOS
brew install google-cloud-sdk

# Other platforms: https://cloud.google.com/sdk/docs/install
```

**Terraform:**
```
# macOS
brew tap hashicorp/tap && brew install hashicorp/tap/terraform

# Other platforms: https://developer.hashicorp.com/terraform/install
```

### Step 2 - Log in

Two separate login commands are required. Run both before starting the wizard.

**User login** (for gcloud CLI commands the wizard runs):
```
gcloud auth login
```

**Application default credentials** (for Terraform's Google provider):
```
gcloud auth application-default login
```

Both open browser windows for authentication.

### Step 3 - Identify your GCP project

```
gcloud projects list
```

The wizard auto-detects the currently active project (`gcloud config get-value project`). If no default project is set, you will be prompted to enter your project ID.

To set a default project in advance:
```
gcloud config set project <YOUR_PROJECT_ID>
```

### Step 4 - Choose your resource names

- **GCP region:** The region for all resources (e.g. `us-central1`, `europe-west1`).
- **Terraform state GCS bucket name:** Must be globally unique (e.g. `myproject-countorsell-tfstate`). The wizard suggests `{project-id}-countorsell-tfstate` as a default. The wizard creates this bucket.

### Step 5 - Add GitHub Actions secrets (optional)

If you are using the included GitHub Actions workflow (`gcp.yml`) to manage infrastructure, add these secrets to your GitHub repository:

| Secret name | Value |
|------------|-------|
| `GCP_SERVICE_ACCOUNT_KEY` | Full contents of a service account key JSON file (for CI use - separate from wizard login) |
| `TF_STATE_BUCKET` | GCS bucket name you chose for Terraform state (without `gs://`) |

To create a service account and key for GitHub Actions:
```
gcloud iam service-accounts create countorsell-terraform \
  --display-name "CountOrSell Terraform" \
  --project <YOUR_PROJECT_ID>

# Grant required roles
gcloud projects add-iam-policy-binding <YOUR_PROJECT_ID> \
  --member "serviceAccount:countorsell-terraform@<YOUR_PROJECT_ID>.iam.gserviceaccount.com" \
  --role roles/editor

# Create key
gcloud iam service-accounts keys create countorsell-terraform-key.json \
  --iam-account countorsell-terraform@<YOUR_PROJECT_ID>.iam.gserviceaccount.com

cat countorsell-terraform-key.json
```

Paste the entire JSON output as the `GCP_SERVICE_ACCOUNT_KEY` secret value.

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| (auto-detected) GCP project ID | From `gcloud config get-value project` |
| GCP project ID | Your project ID (confirmed or entered) |
| GCP region | e.g. `us-central1`, `europe-west1` |
| Terraform state GCS bucket name | Name you choose; wizard creates it (default: `{project-id}-countorsell-tfstate`) |

---

## OAuth Providers

OAuth is configured post-setup from the admin panel. You do not need OAuth credentials to run the wizard. The built-in local admin account (created in wizard Step 9) is always available for local login.

To enable OAuth login after setup, obtain a client ID and client secret from the relevant provider.

### Google

1. Open the [Google Cloud Console](https://console.cloud.google.com/).
2. Navigate to **APIs & Services > Credentials**.
3. Click **Create Credentials > OAuth client ID**.
4. Select **Web application**.
5. Add your instance URL to **Authorized redirect URIs**: `https://<your-domain>/signin-google`
6. Copy the **Client ID** and **Client secret**.

### Microsoft

Supports personal Microsoft accounts (Live/Outlook) and organizational accounts (Entra ID / Azure AD).

1. Open the [Azure portal](https://portal.azure.com/).
2. Navigate to **Microsoft Entra ID > App registrations > New registration**.
3. Set **Supported account types** to **Accounts in any organizational directory and personal Microsoft accounts** to support both Live and Entra ID accounts.
4. Add a redirect URI: `https://<your-domain>/signin-microsoft`
5. After registration, go to **Certificates & secrets > New client secret**. Copy the secret value immediately.
6. Copy the **Application (client) ID** from the Overview tab.

### GitHub

1. Open **GitHub Settings > Developer settings > OAuth Apps > New OAuth App**.
2. Set **Authorization callback URL** to `https://<your-domain>/signin-github`
3. Click **Register application**.
4. On the app page, click **Generate a new client secret**.
5. Copy the **Client ID** and the generated **Client secret**.

---

## TCGPlayer API Key

A TCGPlayer API key enables per-card price refresh from TCGPlayer directly. This is optional. Without a key, the price-refresh feature is not available, but all other pricing from content update packages works normally.

Obtain an API key from the TCGPlayer developer portal. The key is configured in the admin panel post-setup under **Settings > TCGPlayer API Key**. It is stored securely and never exposed in plain text.
