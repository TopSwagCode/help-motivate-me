namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public class DanishEmailTemplates : IEmailTemplates
{
    private const string PrimaryColor = "#4F46E5";
    private const string GrayColor = "#666";
    private const string LightGrayBg = "#F3F4F6";

    public string LoginLinkSubject => "Dit login-link - Help Motivate Me";

    public string GetLoginLinkHtmlBody(string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Log ind på Help Motivate Me</h1>
            <p>Klik på knappen nedenfor for at logge ind på din konto. Dette link udløber om 24 timer.</p>
            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; display: inline-block; font-size: 16px;'>
                    Log ind på Help Motivate Me
                </a>
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                Hvis du ikke har anmodet om dette login-link, kan du trygt ignorere denne e-mail.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                Hvis knappen ikke virker, kopier og indsæt dette link i din browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>
        </body>
        </html>";

    public string GetLoginLinkTextBody(string loginUrl) => $@"Log ind på Help Motivate Me

Klik på linket nedenfor for at logge ind på din konto. Dette link udløber om 24 timer.

{loginUrl}

Hvis du ikke har anmodet om dette login-link, kan du trygt ignorere denne e-mail.";

    public string VerificationSubject => "Bekræft din e-mail - Help Motivate Me";

    public string GetVerificationHtmlBody(string verificationUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Bekræft din e-mail</h1>
            <p>Tak fordi du oprettede en konto hos Help Motivate Me! Bekræft venligst din e-mailadresse for at fuldføre din registrering.</p>
            <p style='margin: 30px 0;'>
                <a href='{verificationUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; display: inline-block; font-size: 16px;'>
                    Bekræft e-mail
                </a>
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                Dette link udløber om 24 timer. Hvis du ikke oprettede en konto, kan du trygt ignorere denne e-mail.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                Hvis knappen ikke virker, kopier og indsæt dette link i din browser:<br/>
                <a href='{verificationUrl}' style='color: {PrimaryColor};'>{verificationUrl}</a>
            </p>
        </body>
        </html>";

    public string GetVerificationTextBody(string verificationUrl) => $@"Bekræft din e-mail

Tak fordi du oprettede en konto hos Help Motivate Me! Bekræft venligst din e-mailadresse for at fuldføre din registrering.

Klik på linket nedenfor for at bekræfte din e-mail:

{verificationUrl}

Dette link udløber om 24 timer. Hvis du ikke oprettede en konto, kan du trygt ignorere denne e-mail.";

    public string GetBuddyInviteSubject(string inviterName) => $"{inviterName} vil have dig som deres ansvarsven!";

    public string GetBuddyInviteHtmlBody(string inviterName, string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Du er blevet inviteret som ansvarsven!</h1>

            <p><strong>{inviterName}</strong> har inviteret dig til at være deres ansvarsven på Help Motivate Me.</p>

            <h2 style='color: #374151; font-size: 18px;'>Hvad er en ansvarsven?</h2>
            <p>En ansvarsven hjælper nogen med at holde sig på sporet med deres mål og vaner. Som ansvarsven kan du:</p>
            <ul>
                <li>Se deres daglige fremskridt (vaner, opgaver og mål)</li>
                <li>Efterlade opmuntrende noter i deres dagbog</li>
                <li>Hjælpe dem med at holde motivationen på deres rejse</li>
            </ul>

            <h2 style='color: #374151; font-size: 18px;'>Sådan bliver du en god ansvarsven</h2>
            <ul>
                <li>Tjek regelmæssigt ind for at se deres fremskridt</li>
                <li>Fejr deres sejre, uanset hvor små</li>
                <li>Tilbyd opmuntring, når de kæmper</li>
                <li>Vær støttende, ikke dømmende</li>
            </ul>

            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    Accepter invitation og se deres fremskridt
                </a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px;'>
                Dette link udløber om 7 dage. Klik på det for at logge ind og se {inviterName}s fremskridt.
            </p>
            <p style='color: {GrayColor}; font-size: 14px;'>
                Hvis knappen ikke virker, kopier og indsæt dette link i din browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>
        </body>
        </html>";

    public string GetBuddyInviteTextBody(string inviterName, string loginUrl) => $@"Du er blevet inviteret som ansvarsven!

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

    public string GetBuddyJournalSubject(string buddyName) => $"{buddyName} har efterladt dig en opmuntrende note!";

    public string GetBuddyJournalHtmlBody(string buddyName, string entryTitle, string journalUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Ny dagbogsindlæg fra din ven!</h1>

            <p>Din ansvarsven <strong>{buddyName}</strong> har skrevet i din dagbog:</p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='font-style: italic; margin: 0;'>""{entryTitle}""</p>
            </div>

            <p style='margin: 30px 0;'>
                <a href='{journalUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    Se hele indlægget
                </a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px;'>
                Bliv ved det gode arbejde! Din ven hepper på dig.
            </p>
        </body>
        </html>";

    public string GetBuddyJournalTextBody(string buddyName, string entryTitle, string journalUrl) => $@"Ny dagbogsindlæg fra din ven!

Din ansvarsven {buddyName} har skrevet i din dagbog:

""{entryTitle}""

Se hele indlægget her:
{journalUrl}

Bliv ved det gode arbejde! Din ven hepper på dig.";

    public string WaitlistSubject => "Du er på ventelisten! - Help Motivate Me";

    public string GetWaitlistHtmlBody(string name) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Du er på ventelisten!</h1>

            <p>Hej {name},</p>

            <p>Tak for din interesse i Help Motivate Me! Vi er i øjeblikket i lukket beta, mens vi finjusterer oplevelsen.</p>

            <p>Du er blevet tilføjet til vores venteliste, og vi giver dig besked, så snart der åbner en plads. Vi inviterer brugere i grupper, mens vi fortsætter med at teste og forbedre produktet.</p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='margin: 0; font-weight: bold;'>Hvad er Help Motivate Me?</p>
                <p style='margin: 10px 0 0 0;'>En produktivitetsapp, der hjælper dig med at sætte meningsfulde mål, opdele dem i handlingsorienterede opgaver og opbygge vaner, der fører til succes.</p>
            </div>

            <p>Vi sætter pris på din tålmodighed og glæder os til at byde dig velkommen snart!</p>

            <p style='color: {GrayColor}; font-size: 14px; margin-top: 30px;'>
                Med venlig hilsen,<br/>
                Help Motivate Me-teamet
            </p>
        </body>
        </html>";

    public string GetWaitlistTextBody(string name) => $@"Du er på ventelisten!

Hej {name},

Tak for din interesse i Help Motivate Me! Vi er i øjeblikket i lukket beta, mens vi finjusterer oplevelsen.

Du er blevet tilføjet til vores venteliste, og vi giver dig besked, så snart der åbner en plads. Vi inviterer brugere i grupper, mens vi fortsætter med at teste og forbedre produktet.

Hvad er Help Motivate Me?
En produktivitetsapp, der hjælper dig med at sætte meningsfulde mål, opdele dem i handlingsorienterede opgaver og opbygge vaner, der fører til succes.

Vi sætter pris på din tålmodighed og glæder os til at byde dig velkommen snart!

Med venlig hilsen,
Help Motivate Me-teamet";

    public string WhitelistSubject => "Du er blevet inviteret til Help Motivate Me!";

    public string GetWhitelistHtmlBody(string loginUrl) => $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h1 style='color: {PrimaryColor};'>Velkommen til Help Motivate Me!</h1>

            <p>Gode nyheder! Du har fået adgang til Help Motivate Me.</p>

            <p>Vi er glade for at have dig med i vores fællesskab af målsættere og vanebyggere. Du kan nu oprette din konto og starte din produktivitetsrejse.</p>

            <p style='margin: 30px 0;'>
                <a href='{loginUrl}'
                   style='background-color: {PrimaryColor}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>
                    Kom i gang
                </a>
            </p>

            <div style='background-color: {LightGrayBg}; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                <p style='margin: 0; font-weight: bold;'>Hvad du kan gøre med Help Motivate Me:</p>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>Sæt meningsfulde mål og følg dine fremskridt</li>
                    <li>Opdel opgaver i håndterbare trin</li>
                    <li>Opbyg daglige, ugentlige og månedlige vaner</li>
                    <li>Skriv dagbog om din rejse og reflekter over din vækst</li>
                </ul>
            </div>

            <p style='color: {GrayColor}; font-size: 14px;'>
                Hvis knappen ikke virker, kopier og indsæt dette link i din browser:<br/>
                <a href='{loginUrl}' style='color: {PrimaryColor};'>{loginUrl}</a>
            </p>

            <p style='color: {GrayColor}; font-size: 14px; margin-top: 30px;'>
                Velkommen ombord!<br/>
                Help Motivate Me-teamet
            </p>
        </body>
        </html>";

    public string GetWhitelistTextBody(string loginUrl) => $@"Velkommen til Help Motivate Me!

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
