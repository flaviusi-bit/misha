# Traveller profile

The applicant aggregate now carries the core traveller profile needed by the ETA critical path.

## Core identity

- First name
- Last name
- Date of birth
- Nationality

## Optional profile data

- Country of birth
- Place of birth
- Gender
- Email
- Phone number

Existing application creation remains backward compatible: an applicant can be created from its external reference and the profile can be completed later through the authenticated applicant profile endpoint.

## API

- `GET /applicants/{id}`
- `PUT /applicants/{id}/profile`

Profile data is stored in PostgreSQL. The external reference remains unique and is not replaced by personal identity fields.
