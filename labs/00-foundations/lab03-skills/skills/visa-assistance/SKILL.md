---
name: visa-assistance
description: Load this skill when users ask about visas, entry requirements, travel documents, or passport validity. Provides comprehensive visa requirements and entry regulations for international destinations.
---

# Visa Assistance Skill

You have access to visa requirement information and entry regulations to help travelers understand what documents they need for international travel. Reference the [visa requirements documentation](references/visa-requirements.md) for detailed country-specific information.

## When to Use This Skill

Use this skill when the traveler:
- Asks about visa requirements for a specific country
- Needs to know about visa-free entry eligibility
- Wants to understand visa application processes
- Asks about passport validity requirements
- Needs information about electronic travel authorizations (eTA)
- Is planning international travel and unsure about entry requirements

## Usage Guidelines

1. **Ask for traveler's nationality** - visa requirements vary significantly by citizenship
2. **Clarify the purpose of travel** - tourist, business, work, or study visas have different requirements
3. **Check trip duration** - visa-free periods have time limits
4. **Verify passport validity** - most countries require 6 months validity beyond travel dates
5. **Consider processing time** - recommend starting visa applications 4-8 weeks before travel
6. **Distinguish between types**:
   - Visa-free entry
   - Electronic visas or travel authorizations (eTA, ESTA, etc.)
   - Traditional visa applications
7. **Provide official resource links** when available

## Example Interactions

**User**: "Do I need a visa to visit Japan?"
**Action**: 
1. Ask for their nationality
2. Explain visa-free entry eligibility or visa requirements
3. Mention passport validity requirements

**User**: "I'm from Australia going to Canada for 2 weeks"
**Action**:
1. Reference VISA_REQUIREMENTS.md for Canada
2. Explain eTA requirement for Australians
3. Provide application process and timeline
4. Mention costs and validity period

**User**: "How long does it take to get a Japanese visa?"
**Action**:
1. Reference visa processing times from VISA_REQUIREMENTS.md
