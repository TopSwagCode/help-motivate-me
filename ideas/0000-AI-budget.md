I want help with new feature "AI Assistant budget"

So right now I log all AI usage in database with: backend/src/HelpMotivateMe.Core/Entities/AiUsageLog.cs

I want to add new field to it to be "EstimatedCost" to it. So every time before making AI call I estimate the cost it would be and after i write to database actual and estimated cost.

The admin page should have a new tab "AI Usage". Top of that page should be total estimated cost + actual cost, cost estimated and actual cost last 30 days. Below should be a list of all AI Usages, Estimated and Actual cost and type (voice / text). For now I will not show which users that has made the actual cost, but rather have overview of what I am spending on AI.

Further more I want "failsafe" that should ensure that I don't loose all my money. I want to add check on AI usage if total cost extends 5$ last 30 days, no AI calls will be made and exception will be thrown. Also I want to make check if user's usage extends 0.25$ or estimaded usage extends 0.25$ last 30 days exception is thrown and no AI calls are made.

So there is total cap shared by all users and there is cap per user. The 2 values for these caps should be set in AppSettings / configurable, so I can set them in env. settings on server.