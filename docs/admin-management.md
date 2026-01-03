# Admin Management

## Making a User an Admin

To grant admin privileges to a user, run the following command:

```bash
docker compose exec -T postgres psql -U postgres -d helpmotivateme -c "UPDATE users SET \"Role\" = 'Admin' WHERE \"Email\" = 'user@example.com' RETURNING \"Username\", \"Email\", \"Role\";"
```

Replace `user@example.com` with the user's email address.

### Alternative: By Username

```bash
docker compose exec -T postgres psql -U postgres -d helpmotivateme -c "UPDATE users SET \"Role\" = 'Admin' WHERE \"Username\" = 'username' RETURNING \"Username\", \"Email\", \"Role\";"
```

## Removing Admin Privileges

To demote an admin back to a regular user:

```bash
docker compose exec -T postgres psql -U postgres -d helpmotivateme -c "UPDATE users SET \"Role\" = 'User' WHERE \"Email\" = 'user@example.com' RETURNING \"Username\", \"Email\", \"Role\";"
```

## Listing All Admins

To see all users with admin privileges:

```bash
docker compose exec -T postgres psql -U postgres -d helpmotivateme -c "SELECT \"Username\", \"Email\", \"Role\", \"CreatedAt\" FROM users WHERE \"Role\" = 'Admin';"
```

## Notes

- Users need to log out and log back in for role changes to take effect
- Once you have one admin, they can manage other users' roles through the Admin Dashboard in the web UI
- The admin dashboard is accessible via the user dropdown menu (only visible to admins)
