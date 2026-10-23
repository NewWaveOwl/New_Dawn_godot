# New Dawn port rules

Original authors: Artiom Sova and Matthew Buzzetti.

## Branches and approval

- Never push directly to main, including force pushes.
- Make changes on a separate branch and submit a pull request.
- Merge a pull request only after Artiom Sova (NewWaveOwl) explicitly approves that pull request and its current changes.
- Do not enable automatic merging or bypass branch protection or review requirements.
- New changes after approval require renewed approval before merging.

## Legacy preservation

- Preserve the original C# source in new_dawn_legacy/ as the reference implementation.
- Preserve existing logic and behavior, including bugs, quirks, calculations, defaults, and edge cases.
- Do not fix, rebalance, simplify, refactor, or otherwise alter legacy logic without explicit permission. Explain the proposed change and wait for approval.
- Make only integration changes required for Godot rendering, input, UI, and platform APIs. Keep adaptations separate from gameplay rules. Request permission if an adaptation changes behavior.
- Reuse existing C# logic wherever possible and compare behavior against the legacy implementation.
- Record suspected bugs and improvement ideas separately; do not implement them without approval.
