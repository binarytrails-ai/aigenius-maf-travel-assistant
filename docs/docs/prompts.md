# Prompts

## Lab 1: Personalization & Memory

### Demo 1A: Natural Profile Building

#### Main Prompt (Recommended)

**Message 1:**
```
I want to plan a trip
```

**Expected Agent Response:**
```
I'd love to help you plan a trip! To give you the best recommendations, could you tell me about your travel preferences?
- What's your travel style? (budget, luxury, mid-range)
- What activities do you enjoy?
- Any past trips you've loved?
```

**Message 2:**
```
I'm a budget backpacker who loves hiking. Last year I went to Portugal and loved it!
```

**Expected:** Agent extracts TravelStyle, Interests, PastDestinations with Rating

---

#### Alternative Variations

**Variation 1 - Adventure Solo**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
I love solo hiking trips. New Zealand last summer was amazing!
```

**Variation 2 - Beach Couple**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
We're budget travelers who love beaches. Thailand last year was incredible!
```

**Variation 3 - Family Travel**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
I travel with my family of 4. We love culture and outdoors. Italy was perfect!
```

**Variation 4 - Luxury Seeker**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
I prefer luxury resorts and spas. Maldives last fall was stunning!
```

**Variation 5 - Food Tourism**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
I'm a foodie who explores local cuisines. Japan last year blew my mind!
```

**Variation 6 - Photography Focus**

**Message 1:**
```
I want to plan a trip
```

**Message 2:**
```
I'm into landscape photography. Iceland last summer was breathtaking!
```

**Expected:** Agent extracts TravelStyle, Interests, PastDestinations with Rating

---

### Demo 1A-FAIL: Without Profile Memory ❌ (Optional - Show the Problem)

**Purpose:** Show what happens WITHOUT user profile memory to demonstrate its value

**Conversation 1:**

**Message 1:**
```
I want to plan a trip
```

**Without Memory - Agent Response:**
```
I'd love to help! To give you the best recommendations, could you tell me:
- What's your travel style? (budget, luxury, mid-range)
- What activities do you enjoy?
- What's your budget range?
- How long do you typically travel?
```

**Message 2:**
```
I'm a budget backpacker who loves hiking. Budget is $2500 for 2 weeks.
```

**Without Memory - Agent Response:**
```
Great! Based on that, I'd recommend Croatia - beautiful hiking trails and budget-friendly!
```

**Conversation 2: (NEW THREAD - Same User)**

**Message 1:**
```
I want to plan another trip
```

**Without Memory - Agent Response:**
```
I'd love to help! To give you the best recommendations, could you tell me:
- What's your travel style? (budget, luxury, mid-range)
- What activities do you enjoy?
- What's your budget range?
- How long do you typically travel?
```

**Problem Demonstrated:** 
- Agent forgot EVERYTHING from previous conversation
- Asks the EXACT SAME questions again
- User must repeat all preferences
- Poor UX, no learning, no memory

**With Profile Memory - Expected (Conversation 2):**
```
Welcome back! I remember you're a budget backpacker who loves hiking ($2500 for 2 weeks).
Ready to plan your next adventure? Where are you thinking?
```

---

### Demo 1B: Immediate Personalization

```
Where should I go next?
```

**Expected:** Agent uses stored profile to recommend Croatia (similar to Portugal, budget-friendly, hiking)

---

### Demo 1B-FAIL: Without Personalization ❌ (Optional)

**Scenario:** Show generic recommendations without profile usage

**User Message:**
```
Where should I go next?
```

**Without Memory - Agent Response:**
```
Where would you like to travel? Some popular destinations include:
- Paris, France
- Tokyo, Japan
- Bali, Indonesia
- New York, USA

What type of experience are you looking for?
```

**Problem:** Generic list, no personalization, asks questions already answered

**With Profile Memory - Expected:**
```
Based on your profile (budget backpacker, loves hiking, loved Portugal):
🇭🇷 Croatia - similar coastal trails, 30% cheaper. Want details?
```

---

### Demo 1C: Cross-Session Memory ✨ (NEW CONVERSATION)

**Instructions:** Start a NEW conversation (different conversation ID)

```
Hey, I need trip ideas
```

**Expected:** Agent recalls profile from previous conversation and personalizes recommendations

---

### Demo 1C-FAIL: Without Cross-Session Memory ❌ (Optional)

**Purpose:** Show what happens when memory doesn't persist across sessions

**Instructions:** Start a NEW conversation (different conversation ID)

**User Message:**
```
Hey, I need trip ideas
```

**Without Cross-Session Memory - Agent Response:**
```
Hi! I'd love to help you plan a trip. Could you tell me:
- What's your travel style? (budget, luxury, mid-range)
- What activities interest you?
- Any past trips you've enjoyed?
- What's your budget range?
```

**Problem:** Agent forgot everything, treats user as new, asks all questions again

**With Cross-Session Memory - Expected:**
```
Welcome back! I see you're a budget backpacker who loves hiking and loved Portugal.
How about Croatia? Similar vibes, great trails, budget-friendly!
```

**Teaching Point:** This demonstrates the WOW factor - persistent memory across sessions!

---

### Demo 1D: Incremental Learning

```
I usually travel for 2 weeks and my budget is around $2500
```

Then follow up with:

```
So where should I go?
```

**Expected:** Agent updates profile with TripDuration and BudgetRange, then refines recommendations

---

### Demo 1E: Memory Recall Questions

```
What's my budget?
```

```
Where did I go that I loved?
```

```
How long do I usually travel for?
```

**Expected:** Agent accurately recalls stored profile information

---

### Demo 1E-FAIL: Without Memory Recall ❌ (Optional)

**Scenario:** Agent can't answer questions about user's profile

**User Message:**
```
What's my budget?
```

**Without Memory - Agent Response:**
```
I don't have that information. Could you tell me what your typical travel budget is?
```

**User Message:**
```
Where did I go that I loved?
```

**Without Memory - Agent Response:**
```
I don't have any record of your past trips. Where have you traveled before?
```

**Problem:** Agent has no memory, can't build trust, user frustrated

**With Profile Memory - Expected:**
```
Your typical budget is $2500 for 2-week trips.
```
```
You went to Portugal and loved it!
```

**Teaching Point:** Memory recall builds trust and transparency

---

## Common Failure Patterns (Quick Reference)

### ❌ Without Profile Memory
- **Repetitive Questions** - Asks same questions every conversation
- **Generic Recommendations** - No personalization or context
- **No Learning** - Doesn't improve with more interactions
- **Poor UX** - User must repeat themselves constantly
- **No Cross-Session Memory** - Every conversation starts from zero

### ✅ With Profile Memory
- **Progressive Learning** - Builds profile incrementally
- **Personalized Suggestions** - Tailored to user preferences
- **Contextual Responses** - References past trips and preferences
- **Persistent Memory** - Remembers across sessions
- **Trust Building** - Can recall what it knows about user

---

## Alternative Prompts (Lab 1 Variations)

### Variation A: Adventure Solo Traveler
```
Hi! I'm into adventure travel and love hiking. I usually travel solo for about 2 weeks at a time. My budget is typically $2000-3000. Last summer I went to New Zealand and loved the Milford Track!
```

### Variation B: Budget Couple
```
Hey! My partner and I are budget travelers who love beaches. We went to Thailand last year and had an amazing time in Krabi. We usually do 10-day trips.
```

### Variation C: Family Trip Planner
```
Hello! I'm planning for my family of 4 (two kids). We love cultural experiences and outdoor activities. We visited Portugal last spring and the kids loved it. We typically travel for 1-2 weeks.
```

### Variation D: Luxury Traveler
```
Hi! I prefer luxury travel experiences. I went to Santorini last fall and it was perfect! I usually take 7-10 day trips with a flexible budget.
```

---

## Lab 2: Long-Term Memory (Vector Store)

### Demo 2A: Semantic Search

```
What restaurants did you recommend in Split?
```

**Expected:** Agent searches vector store and retrieves past conversation about restaurant recommendations

---

### Demo 2B: Long-Term Recall

```
I think you mentioned something about travel insurance last time?
```

**Expected:** Agent finds and recalls specific details from weeks-old conversations

---

## Lab 3: Tools & Structured Data

### Demo 3A: Weather Tool

```
What's the weather like in Croatia in March?
```

**Expected:** Agent calls GetWeatherForecast tool with real data

---

### Demo 3B: LLM Parameter Extraction

```
Find me flights from Seattle to Croatia in March for 2 weeks, budget $2500
```

**Expected:** Agent extracts structured parameters (dates, budget, locations) and calls SearchFlights

---

### Demo 3C: Complex Natural Language

```
Me and my wife want to go somewhere warm next spring for about 10 days, budget is flexible but under three grand
```

**Expected:** Agent extracts NumberOfTravelers=2, calculates spring dates, maxBudget=3000

---

### Demo 3D: Tool Chaining - Calendar Check

```
Book the Delta flight for March 10-20
```

**Expected:** Agent proactively calls CheckCalendarConflicts before booking, detects conflicts, suggests alternatives

---

## Lab 4: Observability

### Demo 4A: Trace SearchFlight

```
Find flights to Paris under $2000
```

**Expected:** View complete trace in Application Insights showing parameter extraction, tool call, costs, latency

---

## Lab 5: Human-in-the-Loop

### Demo 5A: Calendar Entry with Approval

```
Add my Croatia trip to my calendar for March 8-20
```

**Expected:** Agent requests approval, shows calendar entry details, waits for user confirmation before executing

---

## Quick Test Sequence (5 minutes)

Use this rapid-fire sequence to demo all Lab 1 steps quickly:

**Step 1:**
```
I'm a budget backpacker who loves hiking. I went to Portugal last March and loved it!
```

**Step 2:**
```
Where should I go next?
```

**Step 3:** *(New conversation)*
```
Hey! I need vacation ideas
```

**Step 4:**
```
I usually travel for 2 weeks. Budget is $2500.
```
```
So where should I go?
```

**Step 5:**
```
What's my budget?
```
```
Where did I go that I loved?
``` 
