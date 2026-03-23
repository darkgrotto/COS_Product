# Credentials and Secrets Setup Guide

This guide covers everything you need to create or obtain before running the first-run wizard. Each section is specific to one deployment type.

The wizard collects all credentials interactively. This guide explains where those credentials come from and how to prepare them in advance so the wizard runs without interruption.

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
| Docker registry host | Public registry hosting the CountOrSell image (default: `ghcr.io`) |
| Three account passwords | You choose (15+ characters each) |
| Backup destination | Local path or cloud storage credentials (see below) |

### Docker registry

The official CountOrSell image is published at `ghcr.io/darkgrotto/countorsell`. This is a public registry - no authentication is required to pull from it. When the wizard asks for the registry host in Step 3, enter `ghcr.io`.

If you are hosting the image in your own private registry, enter that registry's hostname instead and ensure the host running the containers can authenticate to pull from it.

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

| Item | Created by |
|------|-----------|
| Azure account with an active subscription | You |
| Subscription ID and tenant ID | Your Azure account |
| Service principal (client ID and client secret) | You (instructions below) |
| Resource group name | You choose; Terraform creates it |
| Azure region | You choose |
| State storage: storage account and `tfstate` container | You (instructions below) |

### Step 1 - Find your subscription ID and tenant ID

```
az login
az account show --query "{subscriptionId:id, tenantId:tenantId}"
```

Copy both values. The wizard collects them in Step 4.

### Step 2 - Create a service principal

The service principal is the identity Terraform uses to create Azure resources. It needs Contributor access to your subscription.

```
az ad sp create-for-rbac \
  --name countorsell-terraform \
  --role Contributor \
  --scopes /subscriptions/<YOUR_SUBSCRIPTION_ID>
```

The output looks like:

```json
{
  "appId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "displayName": "countorsell-terraform",
  "password": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "tenant": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

| Output field | Maps to |
|-------------|---------|
| `appId` | Client ID (wizard Step 4, and `AZURE_CLIENT_ID` GitHub secret) |
| `password` | Client secret (wizard Step 4, and `AZURE_CLIENT_SECRET` GitHub secret) |
| `tenant` | Tenant ID (wizard Step 4, and `AZURE_TENANT_ID` GitHub secret) |

**Save the `password` value immediately.** It cannot be retrieved after the command completes.

### Step 3 - Create state storage

Terraform state for Azure is stored in Azure Blob Storage. This storage must exist before running `terraform init`. Create it with the Azure CLI:

```
# Create a dedicated resource group for state (separate from the app resource group)
az group create \
  --name countorsell-tfstate-rg \
  --location eastus

# Create the storage account (name must be globally unique, 3-24 lowercase alphanumeric)
az storage account create \
  --name countorselltfstate \
  --resource-group countorsell-tfstate-rg \
  --location eastus \
  --sku Standard_LRS

# Create the container
az storage container create \
  --name tfstate \
  --account-name countorselltfstate
```

The values you choose here become the `state_resource_group_name` and `state_storage_account_name` Terraform variables, which the wizard collects in Step 4.

### Step 4 - Add GitHub Actions secrets

If you are using the included GitHub Actions workflow (`azure.yml`) to manage infrastructure, add these secrets to your GitHub repository (**Settings > Secrets and variables > Actions**):

| Secret name | Value |
|------------|-------|
| `AZURE_CLIENT_ID` | `appId` from service principal creation |
| `AZURE_CLIENT_SECRET` | `password` from service principal creation |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID |
| `AZURE_TENANT_ID` | `tenant` from service principal creation |

These secrets are only needed for CI/CD automation. They are not required if you run the wizard and Terraform manually.

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| Subscription ID | From `az account show` |
| Tenant ID | From `az account show` or service principal output |
| Client ID | `appId` from service principal |
| Client secret | `password` from service principal |
| Resource group name | Name you choose for the app (Terraform creates it) |
| Azure region | e.g. `eastus`, `westeurope` |
| State resource group name | Resource group containing your state storage account |
| State storage account name | Name of the storage account containing the `tfstate` container |

---

## AWS

### Overview of what you need

| Item | Created by |
|------|-----------|
| AWS account | You |
| IAM user with programmatic access (access key ID and secret access key) | You (instructions below) |
| AWS region | You choose |
| S3 bucket for Terraform state | You (instructions below) |

### Step 1 - Create an IAM user for Terraform

Terraform needs an IAM identity with permissions to create and manage the resources it provisions: App Runner, RDS, Secrets Manager, S3, IAM roles, VPCs, and security groups.

**Option A: Attach `AdministratorAccess` (simplest, least restrictive)**

This is the fastest path but grants broad permissions. Use it for personal or development deployments.

```
aws iam create-user --user-name countorsell-terraform

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/AdministratorAccess
```

**Option B: Attach specific managed policies (more restrictive)**

For production deployments, attach the minimum required policies:

```
aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/AmazonRDSFullAccess

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/AWSAppRunnerFullAccess

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/SecretsManagerReadWrite

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/IAMFullAccess

aws iam attach-user-policy \
  --user-name countorsell-terraform \
  --policy-arn arn:aws:iam::aws:policy/AmazonVPCFullAccess
```

### Step 2 - Create an access key

```
aws iam create-access-key --user-name countorsell-terraform
```

Output:

```json
{
  "AccessKey": {
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
  }
}
```

**Save the `SecretAccessKey` immediately.** It cannot be retrieved after the command completes.

### Step 3 - Create the Terraform state bucket

Terraform state for AWS is stored in S3. The bucket must exist before running `terraform init`. S3 bucket names are globally unique across all AWS accounts.

```
# Replace with a name that is unique to you
aws s3api create-bucket \
  --bucket countorsell-tfstate-yourname \
  --region us-east-1

# Enable versioning (recommended - allows state recovery)
aws s3api put-bucket-versioning \
  --bucket countorsell-tfstate-yourname \
  --versioning-configuration Status=Enabled
```

For regions other than `us-east-1`, add `--create-bucket-configuration LocationConstraint=<region>`:

```
aws s3api create-bucket \
  --bucket countorsell-tfstate-yourname \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1
```

The bucket name becomes the `state_bucket` Terraform variable. The state key defaults to `countorsell/terraform.tfstate` and can be left as-is.

### Step 4 - Add GitHub Actions secrets

If you are using the included GitHub Actions workflow (`aws.yml`) to manage infrastructure, add these secrets to your GitHub repository:

| Secret name | Value |
|------------|-------|
| `AWS_ACCESS_KEY_ID` | `AccessKeyId` from access key creation |
| `AWS_SECRET_ACCESS_KEY` | `SecretAccessKey` from access key creation |
| `AWS_DEFAULT_REGION` | Your chosen AWS region (e.g. `us-east-1`) |

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| AWS access key ID | `AccessKeyId` from access key creation |
| AWS secret access key | `SecretAccessKey` from access key creation |
| AWS region | e.g. `us-east-1`, `eu-west-1` |
| State bucket name | S3 bucket you created for Terraform state |

---

## GCP

### Overview of what you need

| Item | Created by |
|------|-----------|
| GCP account with a project | You |
| Project ID | Your GCP project |
| Service account with appropriate IAM roles | You (instructions below) |
| Service account key file (JSON) | You (instructions below) |
| GCS bucket for Terraform state | You (instructions below) |
| Required APIs enabled | You (instructions below) |

### Step 1 - Find your project ID

```
gcloud projects list
```

Or from the GCP console, the project ID appears in the project selector at the top of the page. It is distinct from the project name and project number.

### Step 2 - Enable required APIs

```
gcloud services enable \
  run.googleapis.com \
  sqladmin.googleapis.com \
  secretmanager.googleapis.com \
  storage.googleapis.com \
  iam.googleapis.com \
  cloudresourcemanager.googleapis.com \
  --project <YOUR_PROJECT_ID>
```

### Step 3 - Create a service account for Terraform

```
gcloud iam service-accounts create countorsell-terraform \
  --display-name "CountOrSell Terraform" \
  --project <YOUR_PROJECT_ID>
```

### Step 4 - Grant IAM roles

```
PROJECT_ID=<YOUR_PROJECT_ID>
SA_EMAIL=countorsell-terraform@${PROJECT_ID}.iam.gserviceaccount.com

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/run.admin

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/cloudsql.admin

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/secretmanager.admin

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/storage.admin

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/iam.serviceAccountAdmin

gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member "serviceAccount:${SA_EMAIL}" \
  --role roles/iam.serviceAccountUser
```

### Step 5 - Create a service account key

```
gcloud iam service-accounts keys create countorsell-terraform-key.json \
  --iam-account countorsell-terraform@<YOUR_PROJECT_ID>.iam.gserviceaccount.com
```

This creates `countorsell-terraform-key.json` in the current directory. The wizard asks for the **path to this file** in Step 4. Store the file somewhere accessible to the wizard process.

**Keep this file secure.** It provides full access to the permissions granted above. Do not commit it to version control.

### Step 6 - Create the Terraform state bucket

Terraform state for GCP is stored in Google Cloud Storage. The bucket must exist before running `terraform init`. GCS bucket names are globally unique.

```
# Replace with a name that is unique to you
gcloud storage buckets create gs://countorsell-tfstate-yourname \
  --project <YOUR_PROJECT_ID> \
  --location us-central1
```

Enable versioning (recommended):

```
gcloud storage buckets update gs://countorsell-tfstate-yourname \
  --versioning
```

The bucket name (without the `gs://` prefix) becomes the `state_bucket` Terraform variable.

### Step 7 - Add GitHub Actions secrets

If you are using the included GitHub Actions workflow (`gcp.yml`) to manage infrastructure, add this secret to your GitHub repository:

| Secret name | Value |
|------------|-------|
| `GCP_SERVICE_ACCOUNT_KEY` | The full contents of `countorsell-terraform-key.json` (the entire JSON object, not the file path) |

To read the file contents:

```
cat countorsell-terraform-key.json
```

Paste the entire output as the secret value.

### Summary of values the wizard collects (Step 4)

| Wizard prompt | Value |
|--------------|-------|
| GCP project ID | Your project ID |
| Service account key file path | Absolute path to `countorsell-terraform-key.json` |
| GCP region | e.g. `us-central1`, `europe-west1` |
| State bucket name | GCS bucket name you created (without `gs://`) |

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
