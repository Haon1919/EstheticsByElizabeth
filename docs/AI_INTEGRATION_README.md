# AI & LLM Integration Guide

This document explains the AI features integrated into the Esthetics by Elizabeth admin panel and how to configure them.

## Features

1.  **Automated Contact Form Triage & Drafting**:
    *   When a client submits a contact form, the AI automatically categorizes it (e.g., "Pricing Question", "Complaint").
    *   It assigns an Urgency Score (1-10) to help you prioritize responses.
    *   It generates a polite, professional draft response that you can copy and paste into your email client.

2.  **Intelligent Client Summaries**:
    *   In the Client Management tab, you can click "Generate AI Summary" for any flagged client.
    *   The AI analyzes their past appointments and flag history to give you a concise 2-3 sentence summary, helping you decide whether to approve or ban them without digging through logs.

3.  **Automated Service Content Generation**:
    *   When adding a new service, you only need to provide the Name and Price.
    *   Click "Generate Description & Aftercare with AI", and the system will write an SEO-friendly description and medically accurate aftercare instructions.

## Configuration

You can configure the AI settings directly from the Admin Panel under **Management -> Settings**.

### Supported Providers

We support two providers: **Google Gemini** and **Azure OpenAI**.

#### Option 1: Google Gemini (Recommended for ease of use)
*   **Pros**: Very easy to get an API key. Often has a generous free tier for developers. Fast response times.
*   **Cons**: Data privacy guarantees on free tiers might not be suitable for highly sensitive medical data (though standard esthetics data is usually fine).
*   **Setup**:
    1. Go to [Google AI Studio](https://aistudio.google.com/).
    2. Create an API Key.
    3. In the Admin Settings, select "Google Gemini", paste the key, and set the model to `gemini-2.5-flash`.

#### Option 2: Azure OpenAI (Recommended for Enterprise/Compliance)
*   **Pros**: Enterprise-grade security and compliance. Data is not used to train base models. Keeps all infrastructure within the Azure ecosystem.
*   **Cons**: Requires applying for access through Microsoft. More complex setup (requires creating a resource and deploying a model). Can be more expensive.
*   **Setup**:
    1. Provision an Azure OpenAI resource in your Azure portal.
    2. Deploy a model (e.g., `gpt-4o`).
    3. In the Admin Settings, select "Azure OpenAI".
    4. Enter your API Key, Endpoint URL, and the Deployment Name you chose.

## Security Note
API keys are stored securely in the database. When viewing the settings page, the key will be masked as `********` to prevent shoulder-surfing.
