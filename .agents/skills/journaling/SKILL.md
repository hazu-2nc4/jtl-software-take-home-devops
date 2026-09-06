---
name: journaling
description: Maintain the repository's AI journey records for each user conversation that involves work in this workspace.
---
# Journaling

Use this skill for every user prompt that concerns this repository.

At the start of work, read `ai-journey/plan.md` and `ai-journey/prompts.md` for the current state and prior decisions.

Before the final response, update the records:

- Append a brief, systematic with number ID entry to `ai-journey/prompts.md` with the user's instruction, the material commands or actions taken, and a brief summary of the outcome. When a user selects, changes, or closes a decision, revise the relevant earlier entry so the record reflects the final decision.
- Update `ai-journey/plan.md` with decisions made in the conversation and the next development steps. Keep completed work separate from upcoming work.
- Preserve `ai-journey/tools.md` as the initialization-only skills snapshot; no need to update it in later conversations.

Record only information relevant to the repository work. Keep entries factual and concise enough for a reviewer to follow the user's judgment and the agent's contribution.
