# ToolPool 
Introducing ToolPool! A tool-sharing platform that connects owners of expensive or cumbersome tools to people who want to use them, but don't own them. Users can make their tools available to others nearby, so renters don't have to deal with the hassle of ownership and maintenance.

### Tech Stack
**Architecture**: Blazor (frontend), ASP.NET Core (backend)

**API's**: Google Maps (location mapping), Sendbird (chat), Stripe (payment), Supabase (database and authentication) 

### How to Run the Web App
1. Open respository in Visual Studio
1. Open `ToolPool.sln`
1. Configure user secrets
    * Right-click **ToolPool** project (`ToolPool/ToolPool`) -> Left-click 'Manage User Secrets'
    * Copy-paste contents of provided `secrets.json` file
    * Repeat for **ToolPool.Client** project (`ToolPool/ToolPool.Client`)
1. Set **ToolPool** (`ToolPool/ToolPool`) as Startup Project
1. Run with `https`


#### Yat Long Chan, Nathan Djunaedi, Xiang Hu, Jude Lopez, Yitao Wang
*Team Gold | Spring 2026 | CS 501 S2 : Agile Mobile Application Development*
