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
| Docker installed and running | Required to mirror the image to ECR (see below) |
| Terraform installed | Install instructions below |
| AWS credentials configured | Any standard AWS credential method (see below) |
| IAM permissions | Must include ECR and all infrastructure permissions (see below) |
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

**Docker:** AWS App Runner only accepts images from Amazon ECR. The wizard pulls the CountOrSell image from `ghcr.io` and pushes it into a private ECR repository in your account before running Terraform. Docker must be installed and running on the machine where you run the wizard.

Install Docker Desktop from https://www.docker.com/products/docker-desktop/ and ensure it is running before starting the wizard.

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

After configuring, verify credentials work:
```
aws sts get-caller-identity
```

### Step 3 - Verify IAM permissions

The credentials must have permission to create all CountOrSell infrastructure. This includes App Runner, RDS, Secrets Manager, S3, IAM roles, VPC resources, and ECR.

#### ECR permissions

The wizard mirrors the application image to a private ECR repository. This requires the following IAM permissions in addition to any Terraform infrastructure permissions the IAM user or role already has.

Create and attach this policy to the IAM user or role before running the wizard:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "ecr:GetAuthorizationToken",
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecr:CreateRepository",
        "ecr:DescribeRepositories",
        "ecr:ListTagsForResource",
        "ecr:TagResource",
        "ecr:DeleteRepository",
        "ecr:BatchCheckLayerAvailability",
        "ecr:BatchGetImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:PutImage",
        "ecr:PutImageScanningConfiguration",
        "ecr:PutImageTagMutability"
      ],
      "Resource": "arn:aws:ecr:<region>:<account-id>:repository/<app-name>"
    }
  ]
}
```

Replace `<region>`, `<account-id>`, and `<app-name>` with your values. `<app-name>` is the sanitized form of the instance name you will enter in wizard Step 7 (lowercase, hyphens only, no spaces).

`ecr:GetAuthorizationToken` must be `Resource: "*"` - this is an AWS requirement; the action has no resource-level restriction.

**To apply this policy in the AWS Console:**
1. Go to **IAM > Policies > Create policy**
2. Switch to the **JSON** tab and paste the policy above (with your values substituted)
3. Name it (e.g. `CountOrSellECR`) and create it
4. Go to **IAM > Users** (or **Roles**), open the user or role used by the wizard, and attach the policy

### Step 4 - Choose your resource names

- **AWS region:** The region for all resources (e.g. `us-east-1`, `eu-west-1`).
- **Terraform state S3 bucket name:** Must be globally unique across all AWS accounts (e.g. `myorg-countorsell-tfstate`). The wizard creates this bucket.

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
