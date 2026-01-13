Dokploy should have 2 deployment slots.

Staging, that deploy everything that is merged to main.

Release branch that is merged main into, that releases to main domain.

That way I can test things first on staging before production and not have that many real deploys.

Also important to add watch, so only when frontend folder changes. 
Or when backend changes.

Or when migrations changes etc. 

This will greatly decrease number of releases.