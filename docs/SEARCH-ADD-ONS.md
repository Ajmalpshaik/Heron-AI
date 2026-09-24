# Search add-ons — what they are, and whether you need them

> | | |
> |---|---|
> | **Type** | Permanent page, in plain words. **It holds no package list and no download sizes** — it points to where each one lives |
> | **For** | Anyone installing Heron, and anyone asking *"why did Heron pick the wrong tool?"* |
> | **Authority** | None of its own. [`requirements-optional.txt`](../requirements-optional.txt) is the list, the code that downloads a thing prints its size, and [`brain/retrieval-history.md`](../brain/retrieval-history.md) holds every measurement. Where this page disagrees with them, **they win** |
> | **Opened** | 2026-09-24, with the first measurement of both add-ons on the owner's own questions |

## The short answer

**Heron works without them.** The basic search is built in and needs nothing installed.

**Both add-ons are optional.** They change how often Heron picks the right tool — better in one way,
worse in another, as the measurement below shows. Nothing breaks without them, and every answer says
which search ran.

## What "search" means in Heron

When you type *"select all ducts"*, Heron has to pick the right tool out of its library. **That pick is
the search.** Heron sends the best few to Claude and Claude chooses from those — so when the right tool
is not in that short list, Claude never sees it.

## The three levels

| Level | What it does | Needs | What the first measurement says |
|---|---|---|---|
| **Basic** | Matches letters and words. It does not understand meaning, and its own code says so | Nothing — built in | It is what runs when nothing else is installed |
| **Trained search model** (`model2vec`) | Understands meaning: *"stop the air going the wrong way"* can reach the flow-direction check without sharing a word with it | A small download, once | **Finds more of your questions** — and sent more *"just asking"* questions to a tool that changes the model. See below |
| **Re-ranker** (`sentence-transformers`) | A second, careful read of the top 20 picks, the question and each description together | The largest optional download in Heron | **Worse at picking tools.** Right on the one standards-document question measured |

**The measurement, 2026-09-24:** your 79 real questions, asked of all three levels at the same library
size. In short — the trained model put the right tool in the **top three for 30** of 64 where basic
search managed 25, and handed a model-changing tool to **9** questions that asked for no change, where
basic search handed one to 5. The re-ranker dropped **right-first from 19 to 12** and raised that 9 to
**15**. Every figure, and what was not measured, is in
[`brain/retrieval-history.md`](../brain/retrieval-history.md) under that date.

**Why a wrong pick is not a wrong edit:** with **Changes** off on the ribbon — the default — a tool that
changes the model cannot run. A wrong pick is a wrong suggestion.

## Are these "AI"?

Small trained helpers with **one job**: score how close your sentence is to each tool's description.
They cannot talk, decide, write code or touch Revit. **They run on your own PC and send nothing
anywhere.** Claude still decides what you meant; these only help the right tool reach it.

## Where they live — and why copying the Heron folder does not carry them

| | |
|---|---|
| **The model files** | Your user's Hugging Face cache — `C:\Users\<you>\.cache\huggingface` on Windows |
| **The Python packages** | Your own Python's user folder. `pip install --user`, so no administrator rights |
| **The Heron folder** | Holds **neither**. A Heron folder copied to another PC needs the add-ons installed there again |
| **GitHub** | Holds the list and the code that uses them. **Never the model files** |

After the first download the files are read from your own PC.

## How to install one on a PC

See what you have first — it names each add-on, what it buys, and what happens without it:

```bash
python tools/check-dependencies.py
```

**The trained search model:**

```bash
pip install --user model2vec
python brain/heron_embed.py        # should answer: Backend: model
```

**The re-ranker — read before you agree.** This prints what it is and how big, before anything downloads:

```bash
python brain/heron_rerank.py
```

The size it prints is a field reading, and a Linux machine downloaded more than it says
([row 5b-200](FRAGMENT-ISSUES.md)); it has not been measured on Windows. If you still want it:
`pip install --user sentence-transformers`, then `python brain/heron_rerank.py --fetch --yes`.

**To take either one away:** `pip uninstall` the same package. Heron goes back to the level below and
says so on every answer.

## What is not decided yet

- **Nothing installs these for you.** The Heron installer does not, and no Python package installs
  automatically today ([`brain/README.md`](../brain/README.md#dependencies)). Whether it should is the
  owner's call.
- **Whether the re-ranker should read standards documents only**, and not tool cards, is a code change
  nobody has made.
- **Why it misreads tools is not settled.** It is given a one-line description of each tool and not the
  tool's example phrases, and its model was trained on web searches. Neither has been tried another way.
