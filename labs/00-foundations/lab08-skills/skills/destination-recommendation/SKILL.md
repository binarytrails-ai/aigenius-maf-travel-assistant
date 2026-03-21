---
name: destination-recommendation
description: Suggests travel destinations based on preferences, budget, season, and interests with detailed information about each location
---

# Destination Recommendation Skill

You have access to destination information and travel planning tools to help travelers discover and choose the perfect destination for their trip. Consult the [destination profiles](references/DESTINATIONS.md) for comprehensive city and attraction information.

## When to Use This Skill

Use this skill when the traveler:
- Is unsure where to travel
- Wants destination suggestions based on specific criteria
- Asks "Where should I go?" or "What are good places to visit?"
- Needs help choosing between multiple destinations
- Wants to understand what a destination offers
- Is looking for destinations that match their interests
- Needs information about best times to visit
- Wants destination comparisons

## Usage Guidelines

1. **Ask clarifying questions** to understand traveler preferences:
   - Budget level (budget, mid-range, luxury)
   - Travel style (adventure, relaxation, cultural, urban, nature)
   - Interests (food, history, shopping, outdoor activities, nightlife)
   - Season or month of travel
   - Duration of trip
   - Traveling solo, couple, family, or group
   - Any must-haves or deal-breakers

2. **Provide personalized recommendations** based on:
   - Weather and seasonal factors
   - Budget compatibility
   - Cultural and activity matches
   - Travel logistics and accessibility

3. **Include practical information**:
   - Best time to visit
   - Typical duration recommended
   - Highlight attractions and experiences
   - General cost level
   - Getting around

4. **Offer comparisons** when presenting multiple options:
   - Highlight key differences
   - Match to stated preferences
   - Explain trade-offs

5. **Consider travel logistics**:
   - Flight availability and duration
   - Visa requirements (reference visa-recommendation skill)
   - Language barriers
   - Safety and accessibility

## Example Interactions

**User**: "Where should I travel in December?"
**Action**:
1. Ask clarifying questions about budget, interests, and preferences
2. Reference DESTINATIONS.md for December-appropriate destinations
3. Suggest 3-4 options with brief highlights
4. Explain why each matches their criteria

**User**: "I want a beach vacation with good food and culture, budget around $3000 for a week"
**Action**:
1. Reference DESTINATIONS.md for beach destinations with food/culture focus
2. Consider mid-range budget level
3. Recommend 2-3 destinations (e.g., Thailand, Bali, Mexico)
4. Explain what each offers and cost expectations
5. Mention best times to visit

**User**: "Tokyo or Singapore?"
**Action**:
1. Ask what's important to them for the trip
2. Reference DESTINATIONS.md for both cities
3. Compare and contrast:
   - Culture and experiences
   - Food scenes
   - Cost differences
   - Weather patterns
   - Activities available
4. Make a recommendation based on their stated priorities

**User**: "What are the best places for solo travelers?"
**Action**:
1. Reference DESTINATIONS.md for solo-friendly destinations
2. Highlight factors important for solo travel:
   - Safety
   - Ease of meeting people
   - Good hostel/social accommodation scene
   - Public transportation
   - English spoken
3. Suggest 3-4 options with specific reasons

## Important Notes

- **Be enthusiastic but realistic** about destinations
- **Don't overpromise** - mention both highlights and considerations
- **Cultural sensitivity** - respect different cultures and customs
- **Safety considerations** - mention if special precautions are needed
- **Seasonal awareness** - weather significantly impacts experience
- **Off-the-beaten-path options** - don't just suggest most popular places
- **Sustainability** - consider mentioning responsible travel practices

## Recommendation Strategy

### For First-Time Travelers
- Suggest well-touristed, easy-to-navigate destinations
- Emphasize safety and English-language availability
- Recommend iconic experiences
- Keep it simple and accessible

### For Experienced Travelers
- Offer lesser-known alternatives
- Suggest deeper cultural experiences
- Can recommend more adventurous options
- Focus on unique or niche experiences

### For Specific Interests

**Food Lovers**: Tokyo, Singapore, Bangkok, Melbourne, Paris, Mexico City
**History Buffs**: Rome, Athens, Cairo, Kyoto, Istanbul
**Beach & Relaxation**: Maldives, Bali, Caribbean islands, Greek islands
**Adventure**: New Zealand, Nepal, Iceland, Patagonia, Costa Rica
**Urban Exploration**: New York, London, Hong Kong, Seoul, Barcelona
**Nature & Wildlife**: Kenya, Galapagos, Alaska, Norway, Madagascar
**Budget Conscious**: Southeast Asia, Eastern Europe, Central America
**Luxury**: Dubai, Maldives, Switzerland, French Riviera, Seychelles

## Proactive Guidance

When recommending destinations:
1. **Mention visa requirements** - suggest checking visa-recommendation skill
2. **Reference weather** - suggest checking weather-info skill for packing
3. **Discuss costs** - suggest checking currency-exchange skill for budgeting
4. **Flight options** - mention that flight-booking skill can help with logistics

## Avoiding Common Pitfalls

- Don't recommend destinations solely based on Instagram popularity
- Consider the traveler's physical abilities and limitations
- Be aware of political situations and travel advisories
- Mention if destinations are particularly crowded during certain seasons
- Acknowledge language barriers where significant
- Consider jet lag for short trips with large time zone changes

## Destination Categories

### City Breaks (3-5 days)
Perfect for: Weekend getaways, first-time visitors, urban explorers

### Beach Destinations (7-14 days)
Perfect for: Relaxation, honeymoons, families, water sports enthusiasts

### Cultural Journeys (10-21 days)
Perfect for: History lovers, cultural immersion, learning experiences

### Adventure Trips (7-21 days)
Perfect for: Active travelers, nature lovers, thrill-seekers

### Multi-City Tours (14-30 days)
Perfect for: Comprehensive exploration, experienced travelers, long vacations

## Best Practices

1. **Start broad, then narrow** - Begin with categories, then specific destinations
2. **Explain the "why"** - Don't just name places, explain what makes them special
3. **Paint a picture** - Help travelers visualize their experience
4. **Be honest about challenges** - Mention language barriers, heat, crowds, etc.
5. **Provide alternatives** - If first choice isn't possible, have backup suggestions
6. **Connect interests to experiences** - Link their hobbies to destination activities
7. **Consider the complete journey** - Factor in flight times, jet lag, connections

## Seasonal Recommendations

### December - February (Northern Winter)
- Southeast Asia: Dry season, excellent weather
- Caribbean: Peak season, great beaches
- Japan: Skiing, winter festivals
- Australia/New Zealand: Summer activities
- Middle East: Comfortable temperatures

### March - May (Spring)
- Europe: Shoulder season, fewer crowds
- Japan: Cherry blossom season
- Mediterranean: Pleasant weather
- USA National Parks: Good conditions

### June - August (Northern Summer)
- Europe: Peak season, festivals
- Scandinavia: Midnight sun
- Iceland: Best weather
- Africa: Wildlife viewing (depends on region)

### September - November (Autumn)
- Europe: Harvest season, fall colors
- India: Post-monsoon
- South America: Spring season
- Middle East: Cooling down

## Follow-up Questions to Ask

- "Is this your first time traveling internationally?"
- "Are you comfortable with language barriers?"
- "Do you prefer fast-paced exploration or relaxed experiences?"
- "What's your tolerance for crowds/tourist areas?"
- "Any dietary restrictions or preferences?"
- "Do you have any accessibility needs?"
- "What's been your favorite trip so far?"
- "Are there any regions you've always wanted to explore?"
