using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MathParser
{
    public static float HandleComplexMath(string expression, float x, float sceneNumber = 0)
    {
        try
        {
            expression = expression.Replace("x", x.ToString());
            expression = expression.Replace("sceneNumber", sceneNumber.ToString());
            List<string> tokens = Tokenize(expression);
            int index = 0;
            float result = ParseExpression(tokens, ref index);
            Debug.Log($"{expression} = {result}");
            return result;
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
            return 0;
        }
    }

    private static List<string> Tokenize(string expr)
    {
        List<string> tokens = new List<string>();
        int i = 0;
        while (i < expr.Length)
        {
            if (char.IsDigit(expr[i]) || expr[i] == '.')
            {
                string num = "";
                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    num += expr[i++];
                tokens.Add(num);
            }
            else if ("+-*/^()>?:".Contains(expr[i]))
            {
                tokens.Add(expr[i].ToString());
                i++;
            }
            else if (char.IsLetter(expr[i]))
            {
                string funcName = "";
                while (i < expr.Length && char.IsLetter(expr[i]))
                    funcName += expr[i++];
                tokens.Add(funcName);
            }
            else
            {
                i++; // Ignore spaces
            }
        }
        return tokens;
    }

    private static float ParseExpression(List<string> tokens, ref int index)
    {
        float result = ParseConditional(tokens, ref index);
        return result;
    }

    private static float ParseConditional(List<string> tokens, ref int index)
    {
        float left = ParseAddSub(tokens, ref index);

        // Check for conditional expression (? :)
        if (index < tokens.Count && tokens[index] == "?")
        {
            index++; // Skip '?'
            float trueValue = ParseAddSub(tokens, ref index);

            if (index >= tokens.Count || tokens[index] != ":")
                throw new Exception("Expected ':' in conditional expression");

            index++; // Skip ':'
            float falseValue = ParseAddSub(tokens, ref index);

            return left > 0 ? trueValue : falseValue;
        }

        return left;
    }

    private static float ParseAddSub(List<string> tokens, ref int index)
    {
        float result = ParseMulDiv(tokens, ref index);
        while (index < tokens.Count && (tokens[index] == "+" || tokens[index] == "-"))
        {
            string op = tokens[index++];
            float nextTerm = ParseMulDiv(tokens, ref index);
            result = op == "+" ? result + nextTerm : result - nextTerm;
        }
        return result;
    }

    private static float ParseMulDiv(List<string> tokens, ref int index)
    {
        float result = ParseFactor(tokens, ref index);
        while (index < tokens.Count && (tokens[index] == "*" || tokens[index] == "/"))
        {
            string op = tokens[index++];
            float nextFactor = ParseFactor(tokens, ref index);
            result = op == "*" ? result * nextFactor : result / nextFactor;
        }
        return result;
    }

    private static float ParseFactor(List<string> tokens, ref int index)
    {
        float result = ParseBase(tokens, ref index);
        while (index < tokens.Count && tokens[index] == "^")
        {
            index++;
            float exponent = ParseFactor(tokens, ref index);
            result = (float)Math.Pow(result, exponent);
        }
        return result;
    }

    private static float ParseBase(List<string> tokens, ref int index)
    {
        // Handle negative numbers and unary minus
        if (tokens[index] == "-")
        {
            index++;
            return -ParseBase(tokens, ref index);
        }

        // Handle functions
        if (char.IsLetter(tokens[index][0]))
        {
            string funcName = tokens[index++];
            if (tokens[index] != "(")
                throw new Exception("Expected '(' after function name");

            index++; // Skip '('
            float argument = ParseExpression(tokens, ref index);

            if (tokens[index] != ")")
                throw new Exception("Expected ')' after function argument");

            index++; // Skip ')'

            if (funcName == "log")
                return (float)Math.Log(argument);
            else if (funcName == "sqrt")
                return (float)Math.Sqrt(argument);
            else if (funcName == "sin")
                return (float)Math.Sin(argument);
            else if (funcName == "cos")
                return (float)Math.Cos(argument);
            else if (funcName == "tan")
                return (float)Math.Tan(argument);
            else if (funcName == "rand")
                return UnityEngine.Random.Range(0f, argument);
            else
                throw new Exception($"Unknown function: {funcName}");
        }

        // Parenthesized expression
        if (tokens[index] == "(")
        {
            index++;
            float result = ParseExpression(tokens, ref index);
            index++; // Skip ')'
            return result;
        }

        // Number
        return float.Parse(tokens[index++]);
    }
}