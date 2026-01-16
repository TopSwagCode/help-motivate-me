namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public class EnglishEmailTemplates : IEmailTemplates
{
    private const string PrimaryColor = "#4F46E5";
    private const string GrayColor = "#666";
    private const string LightGrayBg = "#F3F4F6";

    public string LoginLinkSubject => "Your Login Link - Help Motivate Me";

    public string GetLoginLinkHtmlBody(string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Login to Help Motivate Me</h1>
            <p>Click one of the buttons below to log in to your account. This link will expire in 24 hours and can only be used once.</p>
            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin-right: 10px;'>
                    📱 Open in App
                </a>
                <a href='{loginUrl}'
                   style='background-color: #6B7280; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    🌐 Open in Browser
                </a>
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                <strong>Tip:</strong> Use "Open in App" if you have the app installed. Use "Open in Browser" if you prefer to use your web browser.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                If you didn't request this login link, you can safely ignore this email.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                If the buttons don't work, copy and paste this link into your browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>
        </body>
        </html>";

    public string GetLoginLinkTextBody(string loginUrl) => $@"Login to Help Motivate Me

Click one of the links below to log in to your account. This link will expire in 24 hours and can only be used once.

Open in App: {loginUrl}

Open in Browser: {loginUrl}

Tip: Use the App link if you have the app installed. Use the Browser link if you prefer your web browser.

If you didn't request this login link, you can safely ignore this email.";

    public string GetBuddyInviteSubject(string inviterName) => $"{inviterName} wants you as their accountability buddy!";

    public string GetBuddyInviteHtmlBody(string inviterName, string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>You've Been Invited as an Accountability Buddy!</h1>

            <p><strong>{inviterName}</strong> has invited you to be their accountability buddy on Help Motivate Me.</p>

            <h2 style='color: #374151; font-size: 18px;'>What is an Accountability Buddy?</h2>
            <p>An accountability buddy helps someone stay on track with their goals and habits. As an accountability buddy, you can:</p>
            <ul>
                <li>View their daily progress (habits, tasks, and goals)</li>
                <li>Leave encouraging notes in their journal</li>
                <li>Help them stay motivated on their journey</li>
            </ul>

            <h2 style='color: #374151; font-size: 18px;'>How to Be a Great Accountability Buddy</h2>
            <ul>
                <li>Check in regularly to see their progress</li>
                <li>Celebrate their wins, no matter how small</li>
                <li>Offer encouragement when they're struggling</li>
                <li>Be supportive, not judgmental</li>
            </ul>

            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    Accept Invitation & View Their Progress
                </a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px;'>
                This link will expire in 7 days. Click it to log in and see {inviterName}'s progress.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                If the button doesn't work, copy and paste this link into your browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>
        </body>
        </html>";

    public string GetBuddyInviteTextBody(string inviterName, string loginUrl) => $@"You've Been Invited as an Accountability Buddy!

{inviterName} has invited you to be their accountability buddy on Help Motivate Me.

What is an Accountability Buddy?
An accountability buddy helps someone stay on track with their goals and habits. As an accountability buddy, you can:
- View their daily progress (habits, tasks, and goals)
- Leave encouraging notes in their journal
- Help them stay motivated on their journey

How to Be a Great Accountability Buddy:
- Check in regularly to see their progress
- Celebrate their wins, no matter how small
- Offer encouragement when they're struggling
- Be supportive, not judgmental

Click here to accept the invitation and view their progress:
{loginUrl}

This link will expire in 7 days.";

    public string GetBuddyJournalSubject(string buddyName) => $"{buddyName} left you an encouraging note!";

    public string GetBuddyJournalHtmlBody(string buddyName, string entryTitle, string journalUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>New Journal Entry from Your Buddy!</h1>

            <p>Your accountability buddy <strong>{buddyName}</strong> has written in your journal:</p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='font-style: italic; margin: 0;'>""{entryTitle}""</p>
            </div>

            <p style='margin: 30px 0;'>
                <a href='{journalUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    View Full Entry
                </a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px;'>
                Keep up the great work! Your buddy is cheering you on.
            </p>
        </body>
        </html>";

    public string GetBuddyJournalTextBody(string buddyName, string entryTitle, string journalUrl) => $@"New Journal Entry from Your Buddy!

Your accountability buddy {buddyName} has written in your journal:

""{entryTitle}""

View the full entry here:
{journalUrl}

Keep up the great work! Your buddy is cheering you on.";

    public string WaitlistSubject => "You're on the waitlist! - Help Motivate Me";

    public string GetWaitlistHtmlBody(string name) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>You're on the Waitlist!</h1>

            <p>Hi {name},</p>

            <p>Thank you for your interest in Help Motivate Me! We're currently in closed beta as we refine the experience.</p>

            <p>You've been added to our waitlist and we'll notify you as soon as a spot opens up. We're inviting users in batches as we continue testing and improving the product.</p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='margin: 0; font-weight: bold;'>What is Help Motivate Me?</p>
                <p style='margin: 10px 0 0 0;'>A productivity app that helps you set meaningful goals, break them into actionable tasks, and build habits that lead to success.</p>
            </div>

            <p>We appreciate your patience and look forward to welcoming you soon!</p>

            <p style='color: {GrayColor}; font-size: 14px; margin-top: 30px;'>
                Best regards,<br/>
                The Help Motivate Me Team
            </p>
        </body>
        </html>";

    public string GetWaitlistTextBody(string name) => $@"You're on the Waitlist!

Hi {name},

Thank you for your interest in Help Motivate Me! We're currently in closed beta as we refine the experience.

You've been added to our waitlist and we'll notify you as soon as a spot opens up. We're inviting users in batches as we continue testing and improving the product.

What is Help Motivate Me?
A productivity app that helps you set meaningful goals, break them into actionable tasks, and build habits that lead to success.

We appreciate your patience and look forward to welcoming you soon!

Best regards,
The Help Motivate Me Team";

    public string WhitelistSubject => "You've been invited to Help Motivate Me!";

    public string GetWhitelistHtmlBody(string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Welcome to Help Motivate Me!</h1>

            <p>Great news! You've been granted access to Help Motivate Me.</p>

            <p>We're excited to have you join our community of goal-setters and habit-builders. You can now create your account and start your productivity journey.</p>

            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    Get Started
                </a>
            </p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='margin: 0; font-weight: bold;'>What you can do with Help Motivate Me:</p>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>Set meaningful goals and track your progress</li>
                    <li>Break down tasks into manageable steps</li>
                    <li>Build daily, weekly, and monthly habits</li>
                    <li>Journal your journey and reflect on your growth</li>
                </ul>
            </div>

            <p style='color: {GrayColor}; font-size: 14px;'>
                If the button doesn't work, copy and paste this link into your browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px; margin-top: 30px;'>
                Welcome aboard!<br/>
                The Help Motivate Me Team
            </p>
        </body>
        </html>";

    public string GetWhitelistTextBody(string loginUrl) => $@"Welcome to Help Motivate Me!

Great news! You've been granted access to Help Motivate Me.

We're excited to have you join our community of goal-setters and habit-builders. You can now create your account and start your productivity journey.

Get started here: {loginUrl}

What you can do with Help Motivate Me:
- Set meaningful goals and track your progress
- Break down tasks into manageable steps
- Build daily, weekly, and monthly habits
- Journal your journey and reflect on your growth

Welcome aboard!
The Help Motivate Me Team";
}
