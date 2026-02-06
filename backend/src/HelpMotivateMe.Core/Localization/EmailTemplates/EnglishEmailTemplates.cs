namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public class EnglishEmailTemplates : IEmailTemplates
{
    public string LoginLinkSubject => "Your Login Link - Help Motivate Me";

    public string GetLoginLinkHtmlBody(string loginUrl)
    {
        var content = $@"
            <p>Click the button below to log in to your account. This link will expire in 24 hours.</p>
            {EmailTemplateBase.CreateButton("Login to Help Motivate Me", loginUrl)}
            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                If you didn't request this login link, you can safely ignore this email.
            </p>
            {EmailTemplateBase.CreateFallbackLink(loginUrl)}";

        return EmailTemplateBase.WrapContent("Login to Help Motivate Me", content);
    }

    public string GetLoginLinkTextBody(string loginUrl)
    {
        return $@"Login to Help Motivate Me

Click the link below to log in to your account. This link will expire in 24 hours.

{loginUrl}

If you didn't request this login link, you can safely ignore this email.";
    }

    public string VerificationSubject => "Verify Your Email - Help Motivate Me";

    public string GetVerificationHtmlBody(string verificationUrl)
    {
        var content = $@"
            <p>Thank you for creating an account with Help Motivate Me! Please verify your email address to complete your registration.</p>
            {EmailTemplateBase.CreateButton("Verify Email", verificationUrl)}
            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                This link will expire in 24 hours. If you didn't create an account, you can safely ignore this email.
            </p>
            {EmailTemplateBase.CreateFallbackLink(verificationUrl)}";

        return EmailTemplateBase.WrapContent("Verify Your Email", content);
    }

    public string GetVerificationTextBody(string verificationUrl)
    {
        return $@"Verify Your Email

Thank you for creating an account with Help Motivate Me! Please verify your email address to complete your registration.

Click the link below to verify your email:

{verificationUrl}

This link will expire in 24 hours. If you didn't create an account, you can safely ignore this email.";
    }

    public string GetBuddyInviteSubject(string inviterName)
    {
        return $"{inviterName} wants you as their accountability buddy!";
    }

    public string GetBuddyInviteHtmlBody(string inviterName, string loginUrl)
    {
        var content = $@"
            <p><strong style='color: {EmailTemplateBase.TextPrimary};'>{inviterName}</strong> has invited you to be their accountability buddy on Help Motivate Me.</p>

            <h2 style='color: {EmailTemplateBase.TextPrimary}; font-size: 18px; margin-top: 24px;'>What is an Accountability Buddy?</h2>
            <p>An accountability buddy helps someone stay on track with their goals and habits. As an accountability buddy, you can:</p>
            <ul style='color: {EmailTemplateBase.TextSecondary}; padding-left: 20px;'>
                <li>View their daily progress (habits, tasks, and goals)</li>
                <li>Leave encouraging notes in their journal</li>
                <li>Help them stay motivated on their journey</li>
            </ul>

            <h2 style='color: {EmailTemplateBase.TextPrimary}; font-size: 18px; margin-top: 24px;'>How to Be a Great Accountability Buddy</h2>
            <ul style='color: {EmailTemplateBase.TextSecondary}; padding-left: 20px;'>
                <li>Check in regularly to see their progress</li>
                <li>Celebrate their wins, no matter how small</li>
                <li>Offer encouragement when they're struggling</li>
                <li>Be supportive, not judgmental</li>
            </ul>

            {EmailTemplateBase.CreateButton("Accept Invitation & View Their Progress", loginUrl)}

            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                This link will expire in 7 days. Click it to log in and see {inviterName}'s progress.
            </p>
            {EmailTemplateBase.CreateFallbackLink(loginUrl)}";

        return EmailTemplateBase.WrapContent("You've Been Invited as an Accountability Buddy!", content);
    }

    public string GetBuddyInviteTextBody(string inviterName, string loginUrl)
    {
        return $@"You've Been Invited as an Accountability Buddy!

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
    }

    public string GetBuddyJournalSubject(string buddyName)
    {
        return $"{buddyName} left you an encouraging note!";
    }

    public string GetBuddyJournalHtmlBody(string buddyName, string entryTitle, string journalUrl)
    {
        var content = $@"
            <p>Your accountability buddy <strong style='color: {EmailTemplateBase.TextPrimary};'>{buddyName}</strong> has written in your journal:</p>

            {EmailTemplateBase.CreateQuoteBox(entryTitle)}

            {EmailTemplateBase.CreateButton("View Full Entry", journalUrl)}

            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                Keep up the great work! Your buddy is cheering you on. 🎉
            </p>";

        return EmailTemplateBase.WrapContent("New Journal Entry from Your Buddy!", content);
    }

    public string GetBuddyJournalTextBody(string buddyName, string entryTitle, string journalUrl)
    {
        return $@"New Journal Entry from Your Buddy!

Your accountability buddy {buddyName} has written in your journal:

""{entryTitle}""

View the full entry here:
{journalUrl}

Keep up the great work! Your buddy is cheering you on.";
    }

    public string WaitlistSubject => "You're on the waitlist! - Help Motivate Me";

    public string GetWaitlistHtmlBody(string name)
    {
        var infoContent = $@"
            <p style='margin: 0; font-weight: 700; color: {EmailTemplateBase.TextPrimary};'>What is Help Motivate Me?</p>
            <p style='margin: 10px 0 0 0;'>A productivity app that helps you set meaningful goals, break them into actionable tasks, and build habits that lead to success.</p>";

        var content = $@"
            <p>Hi {name},</p>

            <p>Thank you for your interest in Help Motivate Me! We're currently in closed beta as we refine the experience.</p>

            <p>You've been added to our waitlist and we'll notify you as soon as a spot opens up. We're inviting users in batches as we continue testing and improving the product.</p>

            {EmailTemplateBase.CreateInfoBox(infoContent)}

            <p>We appreciate your patience and look forward to welcoming you soon!</p>

            <p style='color: {EmailTemplateBase.TextMuted}; font-size: 14px; margin-top: 30px;'>
                Best regards,<br/>
                The Help Motivate Me Team
            </p>";

        return EmailTemplateBase.WrapContent("You're on the Waitlist!", content);
    }

    public string GetWaitlistTextBody(string name)
    {
        return $@"You're on the Waitlist!

Hi {name},

Thank you for your interest in Help Motivate Me! We're currently in closed beta as we refine the experience.

You've been added to our waitlist and we'll notify you as soon as a spot opens up. We're inviting users in batches as we continue testing and improving the product.

What is Help Motivate Me?
A productivity app that helps you set meaningful goals, break them into actionable tasks, and build habits that lead to success.

We appreciate your patience and look forward to welcoming you soon!

Best regards,
The Help Motivate Me Team";
    }

    public string WhitelistSubject => "You've been invited to Help Motivate Me!";

    public string GetWhitelistHtmlBody(string loginUrl)
    {
        var featuresContent = $@"
            <p style='margin: 0; font-weight: 700; color: {EmailTemplateBase.TextPrimary};'>What you can do with Help Motivate Me:</p>
            <ul style='margin: 10px 0 0 0; padding-left: 20px; color: {EmailTemplateBase.TextSecondary};'>
                <li>Set meaningful goals and track your progress</li>
                <li>Break down tasks into manageable steps</li>
                <li>Build daily, weekly, and monthly habits</li>
                <li>Journal your journey and reflect on your growth</li>
            </ul>";

        var content = $@"
            <p>Great news! You've been granted access to Help Motivate Me. 🎉</p>

            <p>We're excited to have you join our community of goal-setters and habit-builders. You can now create your account and start your productivity journey.</p>

            {EmailTemplateBase.CreateButton("Get Started", loginUrl)}

            {EmailTemplateBase.CreateInfoBox(featuresContent)}

            {EmailTemplateBase.CreateFallbackLink(loginUrl)}

            <p style='color: {EmailTemplateBase.TextMuted}; font-size: 14px; margin-top: 30px;'>
                Welcome aboard!<br/>
                The Help Motivate Me Team
            </p>";

        return EmailTemplateBase.WrapContent("Welcome to Help Motivate Me!", content);
    }

    public string GetWhitelistTextBody(string loginUrl)
    {
        return $@"Welcome to Help Motivate Me!

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
}