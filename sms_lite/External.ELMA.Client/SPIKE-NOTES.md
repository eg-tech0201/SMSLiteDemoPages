# ELMA Spike Notes

## Objective
Investigate whether SCT can fill the ELMA FO Update Request form automatically from a click action instead of requiring manual copy/paste.

## Current Finding
- Approved interim behavior from Jeff and David on January 22, 2026:
  - use a static link to `https://elma.nass.usda.gov/FO/FOUpdateRequest.aspx`
- No ELMA API specification or supported browser-prefill contract is currently available in this repo.
- Until ELMA confirms a supported integration path, the safe implementation is:
  - keep the static link in SCT
  - build the ELMA client service shell behind it
  - swap the downstream implementation once the real ELMA contract is defined

## Questions Still Open
- Does ELMA expose a supported API for contact update ticket submission?
- If not, is query-string or posted-form prefill officially supported?
- What authentication model is required for server-to-server calls?
- What confirmation/status identifiers should SCT persist?
- What user-facing failure states should be shown when ELMA is unavailable?
