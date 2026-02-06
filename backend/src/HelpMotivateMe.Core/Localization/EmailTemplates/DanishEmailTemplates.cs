namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public class DanishEmailTemplates : IEmailTemplates
{
    public string LoginLinkSubject => "Dit login-link - Help Motivate Me";

    public string GetLoginLinkHtmlBody(string loginUrl)
    {
        var content = $@"
            <p>Klik på knappen nedenfor for at logge ind på din konto. Dette link udløber om 24 timer.</p>
            {EmailTemplateBase.CreateButton("Log ind på Help Motivate Me", loginUrl)}
            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                Hvis du ikke har anmodet om dette login-link, kan du trygt ignorere denne e-mail.
            </p>
            {EmailTemplateBase.CreateFallbackLinkDanish(loginUrl)}";

        return EmailTemplateBase.WrapContent("Log ind på Help Motivate Me", content);
    }

    public string GetLoginLinkTextBody(string loginUrl)
    {
        return $@"Log ind på Help Motivate Me

Klik på linket nedenfor for at logge ind på din konto. Dette link udløber om 24 timer.

{loginUrl}

Hvis du ikke har anmodet om dette login-link, kan du trygt ignorere denne e-mail.";
    }

    public string VerificationSubject => "Bekræft din e-mail - Help Motivate Me";

    public string GetVerificationHtmlBody(string verificationUrl)
    {
        var content = $@"
            <p>Tak fordi du oprettede en konto hos Help Motivate Me! Bekræft venligst din e-mailadresse for at fuldføre din registrering.</p>
            {EmailTemplateBase.CreateButton("Bekræft e-mail", verificationUrl)}
            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                Dette link udløber om 24 timer. Hvis du ikke oprettede en konto, kan du trygt ignorere denne e-mail.
            </p>
            {EmailTemplateBase.CreateFallbackLinkDanish(verificationUrl)}";

        return EmailTemplateBase.WrapContent("Bekræft din e-mail", content);
    }

    public string GetVerificationTextBody(string verificationUrl)
    {
        return $@"Bekræft din e-mail

Tak fordi du oprettede en konto hos Help Motivate Me! Bekræft venligst din e-mailadresse for at fuldføre din registrering.

Klik på linket nedenfor for at bekræfte din e-mail:

{verificationUrl}

Dette link udløber om 24 timer. Hvis du ikke oprettede en konto, kan du trygt ignorere denne e-mail.";
    }

    public string GetBuddyInviteSubject(string inviterName)
    {
        return $"{inviterName} vil have dig som deres ansvarsven!";
    }

    public string GetBuddyInviteHtmlBody(string inviterName, string loginUrl)
    {
        var content = $@"
            <p><strong style='color: {EmailTemplateBase.TextPrimary};'>{inviterName}</strong> har inviteret dig til at være deres ansvarsven på Help Motivate Me.</p>

            <h2 style='color: {EmailTemplateBase.TextPrimary}; font-size: 18px; margin-top: 24px;'>Hvad er en ansvarsven?</h2>
            <p>En ansvarsven hjælper nogen med at holde sig på sporet med deres mål og vaner. Som ansvarsven kan du:</p>
            <ul style='color: {EmailTemplateBase.TextSecondary}; padding-left: 20px;'>
                <li>Se deres daglige fremskridt (vaner, opgaver og mål)</li>
                <li>Efterlade opmuntrende noter i deres dagbog</li>
                <li>Hjælpe dem med at holde motivationen på deres rejse</li>
            </ul>

            <h2 style='color: {EmailTemplateBase.TextPrimary}; font-size: 18px; margin-top: 24px;'>Sådan bliver du en god ansvarsven</h2>
            <ul style='color: {EmailTemplateBase.TextSecondary}; padding-left: 20px;'>
                <li>Tjek regelmæssigt ind for at se deres fremskridt</li>
                <li>Fejr deres sejre, uanset hvor små</li>
                <li>Tilbyd opmuntring, når de kæmper</li>
                <li>Vær støttende, ikke dømmende</li>
            </ul>

            {EmailTemplateBase.CreateButton("Accepter invitation og se deres fremskridt", loginUrl)}

            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                Dette link udløber om 7 dage. Klik på det for at logge ind og se {inviterName}s fremskridt.
            </p>
            {EmailTemplateBase.CreateFallbackLinkDanish(loginUrl)}";

        return EmailTemplateBase.WrapContent("Du er blevet inviteret som ansvarsven!", content);
    }

    public string GetBuddyInviteTextBody(string inviterName, string loginUrl)
    {
        return $@"Du er blevet inviteret som ansvarsven!

{inviterName} har inviteret dig til at være deres ansvarsven på Help Motivate Me.

Hvad er en ansvarsven?
En ansvarsven hjælper nogen med at holde sig på sporet med deres mål og vaner. Som ansvarsven kan du:
- Se deres daglige fremskridt (vaner, opgaver og mål)
- Efterlade opmuntrende noter i deres dagbog
- Hjælpe dem med at holde motivationen på deres rejse

Sådan bliver du en god ansvarsven:
- Tjek regelmæssigt ind for at se deres fremskridt
- Fejr deres sejre, uanset hvor små
- Tilbyd opmuntring, når de kæmper
- Vær støttende, ikke dømmende

Klik her for at acceptere invitationen og se deres fremskridt:
{loginUrl}

Dette link udløber om 7 dage.";
    }

    public string GetBuddyJournalSubject(string buddyName)
    {
        return $"{buddyName} har efterladt dig en opmuntrende note!";
    }

    public string GetBuddyJournalHtmlBody(string buddyName, string entryTitle, string journalUrl)
    {
        var content = $@"
            <p>Din ansvarsven <strong style='color: {EmailTemplateBase.TextPrimary};'>{buddyName}</strong> har skrevet i din dagbog:</p>

            {EmailTemplateBase.CreateQuoteBox(entryTitle)}

            {EmailTemplateBase.CreateButton("Se hele indlægget", journalUrl)}

            <p style='font-size: 14px; color: {EmailTemplateBase.TextMuted};'>
                Bliv ved det gode arbejde! Din ven hepper på dig. 🎉
            </p>";

        return EmailTemplateBase.WrapContent("Ny dagbogsindlæg fra din ven!", content);
    }

    public string GetBuddyJournalTextBody(string buddyName, string entryTitle, string journalUrl)
    {
        return $@"Ny dagbogsindlæg fra din ven!

Din ansvarsven {buddyName} har skrevet i din dagbog:

""{entryTitle}""

Se hele indlægget her:
{journalUrl}

Bliv ved det gode arbejde! Din ven hepper på dig.";
    }

    public string WaitlistSubject => "Du er på ventelisten! - Help Motivate Me";

    public string GetWaitlistHtmlBody(string name)
    {
        var infoContent = $@"
            <p style='margin: 0; font-weight: 700; color: {EmailTemplateBase.TextPrimary};'>Hvad er Help Motivate Me?</p>
            <p style='margin: 10px 0 0 0;'>En produktivitetsapp, der hjælper dig med at sætte meningsfulde mål, opdele dem i handlingsorienterede opgaver og opbygge vaner, der fører til succes.</p>";

        var content = $@"
            <p>Hej {name},</p>

            <p>Tak for din interesse i Help Motivate Me! Vi er i øjeblikket i lukket beta, mens vi finjusterer oplevelsen.</p>

            <p>Du er blevet tilføjet til vores venteliste, og vi giver dig besked, så snart der åbner en plads. Vi inviterer brugere i grupper, mens vi fortsætter med at teste og forbedre produktet.</p>

            {EmailTemplateBase.CreateInfoBox(infoContent)}

            <p>Vi sætter pris på din tålmodighed og glæder os til at byde dig velkommen snart!</p>

            <p style='color: {EmailTemplateBase.TextMuted}; font-size: 14px; margin-top: 30px;'>
                Med venlig hilsen,<br/>
                Help Motivate Me-teamet
            </p>";

        return EmailTemplateBase.WrapContent("Du er på ventelisten!", content);
    }

    public string GetWaitlistTextBody(string name)
    {
        return $@"Du er på ventelisten!

Hej {name},

Tak for din interesse i Help Motivate Me! Vi er i øjeblikket i lukket beta, mens vi finjusterer oplevelsen.

Du er blevet tilføjet til vores venteliste, og vi giver dig besked, så snart der åbner en plads. Vi inviterer brugere i grupper, mens vi fortsætter med at teste og forbedre produktet.

Hvad er Help Motivate Me?
En produktivitetsapp, der hjælper dig med at sætte meningsfulde mål, opdele dem i handlingsorienterede opgaver og opbygge vaner, der fører til succes.

Vi sætter pris på din tålmodighed og glæder os til at byde dig velkommen snart!

Med venlig hilsen,
Help Motivate Me-teamet";
    }

    public string WhitelistSubject => "Du er blevet inviteret til Help Motivate Me!";

    public string GetWhitelistHtmlBody(string loginUrl)
    {
        var featuresContent = $@"
            <p style='margin: 0; font-weight: 700; color: {EmailTemplateBase.TextPrimary};'>Hvad du kan gøre med Help Motivate Me:</p>
            <ul style='margin: 10px 0 0 0; padding-left: 20px; color: {EmailTemplateBase.TextSecondary};'>
                <li>Sæt meningsfulde mål og følg dine fremskridt</li>
                <li>Opdel opgaver i håndterbare trin</li>
                <li>Opbyg daglige, ugentlige og månedlige vaner</li>
                <li>Skriv dagbog om din rejse og reflekter over din vækst</li>
            </ul>";

        var content = $@"
            <p>Gode nyheder! Du har fået adgang til Help Motivate Me. 🎉</p>

            <p>Vi er glade for at have dig med i vores fællesskab af målsættere og vanebyggere. Du kan nu oprette din konto og starte din produktivitetsrejse.</p>

            {EmailTemplateBase.CreateButton("Kom i gang", loginUrl)}

            {EmailTemplateBase.CreateInfoBox(featuresContent)}

            {EmailTemplateBase.CreateFallbackLinkDanish(loginUrl)}

            <p style='color: {EmailTemplateBase.TextMuted}; font-size: 14px; margin-top: 30px;'>
                Velkommen ombord!<br/>
                Help Motivate Me-teamet
            </p>";

        return EmailTemplateBase.WrapContent("Velkommen til Help Motivate Me!", content);
    }

    public string GetWhitelistTextBody(string loginUrl)
    {
        return $@"Velkommen til Help Motivate Me!

Gode nyheder! Du har fået adgang til Help Motivate Me.

Vi er glade for at have dig med i vores fællesskab af målsættere og vanebyggere. Du kan nu oprette din konto og starte din produktivitetsrejse.

Kom i gang her: {loginUrl}

Hvad du kan gøre med Help Motivate Me:
- Sæt meningsfulde mål og følg dine fremskridt
- Opdel opgaver i håndterbare trin
- Opbyg daglige, ugentlige og månedlige vaner
- Skriv dagbog om din rejse og reflekter over din vækst

Velkommen ombord!
Help Motivate Me-teamet";
    }
}