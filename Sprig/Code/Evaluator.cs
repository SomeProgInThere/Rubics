
using System.Collections.Immutable;
using Sprig.Code.Binding;
using Sprig.Code.Syntax;

namespace Sprig.Code;

internal sealed class Evaluator(BoundStatement? root, Dictionary<VariableSymbol, object> variables) {
    
    public object? Evaluate() {
        EvaluateStatement(root);
        return lastValue;
    }

    private void EvaluateStatement(BoundStatement? node) {
        switch (node?.Kind) {
            case BoundKind.BlockStatement:
                EvaluateBlockStatement((BoundBlockStatement)node);
                break;
            
            case BoundKind.VariableDeclarationStatement:
                EvaluateVariableDeclaration((BoundVariableDeclarationStatement)node);
                break;

            case BoundKind.AssignOperationStatement:
                EvaluateAssignOperationStatement((BoundAssignOperationStatement)node);
                break;

            case BoundKind.ExpressionStatement:
                EvaluateExpressionStatement((BoundExpressionStatement)node);
                break;

            case BoundKind.IfStatement:
                EvaluateIfStatement((BoundIfStatement)node);
                break;

            case BoundKind.WhileStatement:
                EvaluateWhileStatement((BoundWhileStatement)node);
                break;

            default: 
                throw new Exception($"Undefined statement: {node?.Kind}");
        }
    }

    private void EvaluateAssignOperationStatement(BoundAssignOperationStatement node) {
        var value = (int)EvaluateExpression(node.Expression);

        if (node.Variable == null) 
            return;
        
        var kind = node.AssignOperatorToken.Kind;
        variables[node.Variable] = kind switch {
            SyntaxKind.PlusEqualsToken          => (int)variables[node.Variable] + value,
            SyntaxKind.MinusEqualsToken         => (int)variables[node.Variable] - value,
            SyntaxKind.StarEqualsToken          => (int)variables[node.Variable] * value,
            SyntaxKind.SlashEqualsToken         => (int)variables[node.Variable] / value,
            SyntaxKind.PercentEqualsToken       => (int)variables[node.Variable] % value,
            SyntaxKind.AmpersandEqualsToken     => (int)variables[node.Variable] & value,
            SyntaxKind.PipeEqualsToken          => (int)variables[node.Variable] | value,
            SyntaxKind.CircumflexEqualsToken    => (int)variables[node.Variable] ^ value,
                
            _ => throw new Exception($"Undefined assignment operator: {kind}"),
        };

        lastValue = variables[node.Variable];
    }

    private void EvaluateBlockStatement(BoundBlockStatement node) {
        foreach (var boundStatement in node.Statements)
            EvaluateStatement(boundStatement);
    }

    private void EvaluateVariableDeclaration(BoundVariableDeclarationStatement node) {
        var value = EvaluateExpression(node.Initializer);
        variables[node.Variable] = value;
        lastValue = value;
    }
    
    private void EvaluateExpressionStatement(BoundExpressionStatement node) => lastValue = EvaluateExpression(node.Expression);
    
    private void EvaluateIfStatement(BoundIfStatement node) {
        var condition = (bool)EvaluateExpression(node.Condition);
        
        if (condition)
            EvaluateStatement(node.IfStatement);
        else if (node.ElseStatement != null)
            EvaluateStatement(node.ElseStatement);
    }

    private void EvaluateWhileStatement(BoundWhileStatement node) {        
        while ((bool)EvaluateExpression(node.Condition))
            EvaluateStatement(node.Body);
    }

    private object EvaluateExpression(BoundExpression? node) {
        return node?.Kind switch {
            BoundKind.LiteralExpression     => EvaluateLiteralExpression((BoundLiteralExpression)node),
            BoundKind.VariableExpression    => EvaluateVariableExpression((BoundVariableExpression)node),
            BoundKind.AssignmentExpression  => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
            BoundKind.UnaryExpression       => EvaluateUnaryExpression((BoundUnaryExpression)node),
            BoundKind.BinaryExpression      => EvaluateBinaryExpression((BoundBinaryExpression)node),
            BoundKind.RangeExpression       => EvaluateRangeExpression((BoundRangeExpression)node),
            
            _ => throw new Exception($"Undefined node: {node?.Kind}"),
        };
    }

    private static object EvaluateLiteralExpression(BoundLiteralExpression literal) => literal.Value;
    
    private object EvaluateVariableExpression(BoundVariableExpression node) {
        if (node.Variable is null)
            throw new Exception($"Variable: {nameof(node.Variable)} is not initialized");
        
        return variables[node.Variable];
    }

    private object EvaluateAssignmentExpression(BoundAssignmentExpression node) {
        var value = EvaluateExpression(node.Expression);
        variables[node.Variable] = value;
        return value;
    }

    private object EvaluateUnaryExpression(BoundUnaryExpression node) {
        var operand = EvaluateExpression(node.Operand);

        switch (node.Operator.Kind) {
            case UnaryOperatorKind.Identity:
                return (int)operand;
                
            case UnaryOperatorKind.Negetion:
                return -(int)operand;

            case UnaryOperatorKind.BitwiseNot:
                return ~(int)operand;

            case UnaryOperatorKind.LogicalNot:
                return !(bool)operand;

            case UnaryOperatorKind.PostIncrement: {
                var value = (int)operand;
                value++;
                return value;
            }

            case UnaryOperatorKind.PostDecrement: {
                var value = (int)operand;
                value--;
                return value;
            }
                
            case UnaryOperatorKind.PreIncrement: {
                var value = (int)operand;
                ++value;
                return value;
            }
            
            case UnaryOperatorKind.PreDecrement: {
                var value = (int)operand;
                --value;
                return value;
            }
            
            default:
                throw new Exception($"Unexpected Unary operator: {node.Operator}");
        }
    }

    private object EvaluateBinaryExpression(BoundBinaryExpression node) {
        var left = EvaluateExpression(node.Left);
        var right = EvaluateExpression(node.Right);

        return node.Operator.Kind switch {
            BinaryOperatorKind.Add      => (int)left + (int)right,
            BinaryOperatorKind.Substact => (int)left - (int)right,
            BinaryOperatorKind.Multiply => (int)left * (int)right,
            BinaryOperatorKind.Divide   => (int)left / (int)right,
            BinaryOperatorKind.Modulus  => (int)left % (int)right,

            BinaryOperatorKind.Remainder    => Math.DivRem((int)left, (int)right).Remainder,
            BinaryOperatorKind.RaisePower   => (int)Math.Pow((int)left, (int)right),

            BinaryOperatorKind.LogicalAnd => (bool)left && (bool)right,
            BinaryOperatorKind.LogicalOr  => (bool)left || (bool)right,

            BinaryOperatorKind.BitwiseAnd => (int)left & (int)right,
            BinaryOperatorKind.BitwiseOr  => (int)left | (int)right,
            BinaryOperatorKind.BitwiseXor => (int)left ^ (int)right,

            BinaryOperatorKind.BitshiftLeft  => (int)left >> (int)right,
            BinaryOperatorKind.BitshiftRight => (int)left << (int)right,

            BinaryOperatorKind.GreaterThan          => (int)left >  (int)right,
            BinaryOperatorKind.GreaterThanEqualsTo  => (int)left >= (int)right,
            BinaryOperatorKind.LesserThan           => (int)left <  (int)right,
            BinaryOperatorKind.LesserThanEqualsTo   => (int)left <= (int)right,

            BinaryOperatorKind.Equals    => Equals(left, right),
            BinaryOperatorKind.NotEquals => !Equals(left, right),

            _ => throw new Exception($"Unexpected Binary operator: {node.Operator}"),
        };
    }

    private object EvaluateRangeExpression(BoundRangeExpression node) {
        var lower = EvaluateExpression(node.Lower);
        var upper = EvaluateExpression(node.Upper);
        return (lower, upper);
    }

    private object? lastValue;
}

public sealed class EvaluationResult(ImmutableArray<DiagnosticMessage> diagnostics, object? result = null) {
    public ImmutableArray<DiagnosticMessage> Diagnostics { get; } = diagnostics;
    public object? Result { get; } = result;
}

public sealed class VariableSymbol(string name, bool mutable, Type type) {
    public string Name { get; } = name;
    public bool Mutable { get; } = mutable;
    public Type Type { get; } = type;
}