// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Turns runaway recursion in a fragment into a FINDING instead of a dead
    /// Revit.
    ///
    /// THE HOLE THIS CLOSES. RunScript catches Exception and its comment says
    /// "a fragment that throws is a finding, not a crash". That is true of
    /// every exception except one. StackOverflowException cannot be caught -
    /// since .NET 2.0 the runtime fails fast on it, no handler runs, no
    /// finally runs, nothing is written and nothing is asked. A fragment that
    /// recurses without a floor does not return an error. It ends the process
    /// it is running in, and that process is REVIT, holding the user's unsaved
    /// model.
    ///
    /// RuntimeHelpers.EnsureSufficientExecutionStack() throws a CATCHABLE
    /// InsufficientExecutionStackException just before the stack would
    /// actually run out. Injected at the top of every body that recursion must
    /// pass through, it converts the uncatchable failure into the ordinary one
    /// RunScript already handles.
    ///
    /// WHY THIS GUARDS LAMBDAS WHEN THE IDEA IT CAME FROM DELIBERATELY DOES
    /// NOT. The technique is well known and the usual advice is to guard named
    /// members and leave anonymous functions alone, on the grounds that
    /// recursion flowing only through lambdas is not a real-world shape. THAT
    /// REASONING IS CORRECT FOR ORDINARY C# AND WRONG HERE. A Heron fragment
    /// is a SCRIPT: measured 2026-09-16 across all 372 fragments, NOT ONE
    /// declares a method or a local function. They are top-level statements
    /// and `Func<>` / `Action<>` lambdas, so guarding named members only would
    /// guard nothing at all in this repository. Taking that rule unexamined
    /// would have produced a guard that compiles, passes review, and protects
    /// against nothing.
    ///
    /// WHAT IS DELIBERATELY LEFT ALONE, and it is a real gap rather than an
    /// oversight: an EXPRESSION-bodied lambda (`x => expr`). Rewriting one
    /// into a statement lambda changes what it converts to - Func versus
    /// Expression - and can silently pick a different overload. A fragment
    /// that breaks its own recursion entirely through expression-bodied
    /// lambdas is still unguarded. Nothing in the library does that today, and
    /// the trade is deliberate: a guard that occasionally misses is worth far
    /// more than one that changes what working code means.
    ///
    /// LINE NUMBERS SURVIVE. The injected statement carries no trivia of its
    /// own, so it shares a line with the brace it follows and every original
    /// statement keeps its line - and it goes BEFORE that brace's line
    /// ending, not after it, so a #if opening the body still begins its line
    /// (see AtHead). That matters because Compile() reports compiler
    /// diagnostics with a prologue offset and tells the reader to subtract it
    /// to find the line in fragment.cs. Column positions within that one line
    /// do move, which is why this must never be used for anything that maps
    /// character offsets.
    ///
    /// IT MUST NEVER BE THE REASON A FRAGMENT STOPS WORKING. Compile() calls
    /// Guard() inside a try and falls back to the original source if anything
    /// at all goes wrong. A rewriter that hardens the common case and breaks
    /// the rare one has made the trade backwards.
    ///
    /// IT ALSO PUTS THE RUN CLOCK IN, since 2026-10-17 (D-116). A loop that
    /// never ends is the other way a fragment takes Revit down with it - not
    /// by crashing it, by never handing the thread back. So beside the stack
    /// check at the entry of every body above, and at the head of every loop
    /// body - for, foreach, a deconstructing foreach, while and do - goes a
    /// call to HeronRunClock.Check(), which throws when the job is out of
    /// time or Heron was told to stop. A loop whose body is one statement
    /// gets braces round it, carrying that statement's own trivia so that no
    /// line moves. Recursion needs no loop of its own: every call passes the
    /// entry check. What stays unchecked is what stays unguarded above - an
    /// expression-bodied lambda, and so every LINQ query - and a loop made
    /// with goto, which no fragment writes today.
    /// </summary>
    internal sealed class HeronStackGuard : CSharpSyntaxRewriter
    {
        /// <summary>
        /// Fully qualified with `global::` so it needs no using directive and
        /// cannot be captured by a type a fragment happens to declare.
        /// </summary>
        private const string GuardText =
            "global::System.Runtime.CompilerServices.RuntimeHelpers"
            + ".EnsureSufficientExecutionStack();";

        /// <summary>
        /// The run clock's check, qualified the same way. HeronRunClock is a
        /// public top-level type of the add-in's assembly, which every script
        /// already references because its globals type lives there.
        /// </summary>
        private const string ClockText =
            "global::Heron.Revit.Addin.HeronRunClock.Check();";

        private static StatementSyntax Guard()
        {
            return SyntaxFactory.ParseStatement(GuardText)
                .WithLeadingTrivia()
                .WithTrailingTrivia();
        }

        private static StatementSyntax Clock()
        {
            return SyntaxFactory.ParseStatement(ClockText)
                .WithLeadingTrivia()
                .WithTrailingTrivia();
        }

        /// <summary>
        /// The fragment's source with the guard injected, or the source
        /// unchanged if it cannot be parsed or rewritten.
        ///
        /// Parsed as a SCRIPT, which is how CSharpScript will parse it too - a
        /// fragment's top-level statements are not legal in a regular
        /// compilation unit and would come back as a tree full of errors.
        /// </summary>
        public static string Apply(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;

            try
            {
                var options = new CSharpParseOptions(kind: SourceCodeKind.Script);
                var tree = CSharpSyntaxTree.ParseText(source, options);

                // A fragment that does not parse is a compile error, and
                // Compile() is about to report it far better than this can.
                // Rewriting a broken tree could only make that message worse.
                if (tree.GetDiagnostics().GetEnumerator().MoveNext())
                    return source;

                var rewritten = new HeronStackGuard().Visit(tree.GetRoot());
                return rewritten == null ? source : rewritten.ToFullString();
            }
            catch (Exception)
            {
                // Deliberately silent and deliberately total. The guard is a
                // safety net; a safety net that can itself fail the run is a
                // hazard. If it cannot be applied, the fragment runs exactly
                // as it did before this class existed.
                return source;
            }
        }

        private static BlockSyntax Prepend(BlockSyntax body)
        {
            return AtHead(body, Guard(), Clock());
        }

        /// <summary>
        /// The statements put first in a block, ON THE BRACE'S OWN LINE.
        ///
        /// The opening brace hands its trailing trivia - its line ending, and
        /// any comment before it - to the last statement put in, so the
        /// injected text sits between the brace and the end of that line.
        /// Placed after it instead, it would share a line with whatever opens
        /// the body, and when that is an #if the directive is no longer first
        /// on its line: CS1040, a fragment that compiled yesterday and does
        /// not today. Measured 2026-10-17 on find-untagged-elements and
        /// tag-elements-in-view, whose foreach bodies open with
        /// `#if REVIT2020 || REVIT2021`. Trivia is moved, never added.
        /// </summary>
        private static BlockSyntax AtHead(BlockSyntax body, params StatementSyntax[] first)
        {
            var open = body.OpenBraceToken;
            first[first.Length - 1] = first[first.Length - 1]
                .WithTrailingTrivia(open.TrailingTrivia);
            return body
                .WithOpenBraceToken(open.WithTrailingTrivia())
                .WithStatements(body.Statements.InsertRange(0, first));
        }

        /// <summary>
        /// A loop body with the clock's check at its head. A body that is one
        /// statement becomes a block of two, and the statement's own leading
        /// and trailing trivia move out onto the new braces - so its comments,
        /// its newline and any #if around it stay where they were, and nothing
        /// is added that could move a line.
        /// </summary>
        private static StatementSyntax Checked(StatementSyntax body)
        {
            var block = body as BlockSyntax;
            if (block != null) return AtHead(block, Clock());

            var open = SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                .WithLeadingTrivia(body.GetLeadingTrivia())
                .WithTrailingTrivia();
            var close = SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                .WithLeadingTrivia()
                .WithTrailingTrivia(body.GetTrailingTrivia());
            var inner = body.WithLeadingTrivia().WithTrailingTrivia();

            return SyntaxFactory.Block(open,
                SyntaxFactory.List(new[] { Clock(), inner }), close);
        }

        // ---- the shapes a Heron fragment actually contains ----

        public override SyntaxNode VisitParenthesizedLambdaExpression(
            ParenthesizedLambdaExpressionSyntax node)
        {
            var visited = (ParenthesizedLambdaExpressionSyntax)
                base.VisitParenthesizedLambdaExpression(node);
            return visited.Block == null
                ? (SyntaxNode)visited
                : visited.WithBlock(Prepend(visited.Block));
        }

        public override SyntaxNode VisitSimpleLambdaExpression(
            SimpleLambdaExpressionSyntax node)
        {
            var visited = (SimpleLambdaExpressionSyntax)
                base.VisitSimpleLambdaExpression(node);
            return visited.Block == null
                ? (SyntaxNode)visited
                : visited.WithBlock(Prepend(visited.Block));
        }

        public override SyntaxNode VisitAnonymousMethodExpression(
            AnonymousMethodExpressionSyntax node)
        {
            var visited = (AnonymousMethodExpressionSyntax)
                base.VisitAnonymousMethodExpression(node);
            return visited.Block == null
                ? (SyntaxNode)visited
                : visited.WithBlock(Prepend(visited.Block));
        }

        // ---- shapes no fragment uses today, guarded so the first one that
        // ---- does is not silently unprotected ----

        public override SyntaxNode VisitLocalFunctionStatement(
            LocalFunctionStatementSyntax node)
        {
            var visited = (LocalFunctionStatementSyntax)
                base.VisitLocalFunctionStatement(node);
            return visited.Body == null
                ? (SyntaxNode)visited
                : visited.WithBody(Prepend(visited.Body));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            return visited.Body == null
                ? (SyntaxNode)visited
                : visited.WithBody(Prepend(visited.Body));
        }

        // ---- every loop, for the run clock (D-116). A fragment that loops
        // ---- for ever goes round one of these, so each pass asks the clock.

        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            var visited = (ForStatementSyntax)base.VisitForStatement(node);
            return visited.WithStatement(Checked(visited.Statement));
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node);
            return visited.WithStatement(Checked(visited.Statement));
        }

        public override SyntaxNode VisitForEachVariableStatement(
            ForEachVariableStatementSyntax node)
        {
            var visited = (ForEachVariableStatementSyntax)
                base.VisitForEachVariableStatement(node);
            return visited.WithStatement(Checked(visited.Statement));
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            var visited = (WhileStatementSyntax)base.VisitWhileStatement(node);
            return visited.WithStatement(Checked(visited.Statement));
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
        {
            var visited = (DoStatementSyntax)base.VisitDoStatement(node);
            return visited.WithStatement(Checked(visited.Statement));
        }
    }
}
