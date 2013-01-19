﻿module MiniC.Compiler.Parser

open Piglet.Parser
open Ast
open ParsingUtilities

let configurator = ParserFactory.Configure<obj>()

// Non-terminals

let nonTerminal<'T> () = new NonTerminalWrapper<'T>(configurator.CreateNonTerminal())

let program               = nonTerminal<Program>()
let declarationList       = nonTerminal<Declaration list>()
let declaration           = nonTerminal<Declaration>()
let functionDeclaration   = nonTerminal<Declaration>()
let compoundStatement     = nonTerminal<CompoundStatement>()
let typeSpec              = nonTerminal<TypeSpec>()
let parameters            = nonTerminal<Parameters>()
let parameterList         = nonTerminal<Parameters>()
let parameter             = nonTerminal<Parameter>()
let optionalStatementList = nonTerminal<Statement list>()
let statementList         = nonTerminal<Statement list>()
let statement             = nonTerminal<Statement>()
let expressionStatement   = nonTerminal<ExpressionStatement>()
let expression            = nonTerminal<Expression>()
let returnStatement       = nonTerminal<Expression option>()

// Terminals

let terminalParse<'T> regex (onParse : (string -> 'T)) =
    new TerminalWrapper<'T>(configurator.CreateTerminal(regex, (fun s -> box (onParse s))))

let terminal regex =
    new TerminalWrapper<string>(configurator.CreateTerminal(regex))

let returnKeyword = terminal      "return"
let voidKeyword   = terminal      "void"
let plus          = terminal      @"\+"
let asterisk      = terminal      @"\*"
let number        = terminalParse @"\d+"  (fun s -> Ast.IntLiteral(int32 s))
let trueLiteral   = terminalParse "true"  (fun s -> Ast.BoolLiteral(true))
let falseLiteral  = terminalParse "false" (fun s -> Ast.BoolLiteral(false))
let intKeyword    = terminalParse "int"   (fun s -> Ast.Int)
let identifier    = terminalParse @"\w+"  (fun s -> s)
let openParen     = terminal      @"\("
let closeParen    = terminal      @"\)"
let openCurly     = terminal      @"\{"
let closeCurly    = terminal      @"\}"
let semicolon     = terminal      ";"

// Productions

program.AddProduction(declarationList).SetReduceToFirst()

declarationList.AddProduction(declarationList, declaration).SetReduceFunction (fun x y -> x @ [y])
declarationList.AddProduction(declaration).SetReduceFunction (fun x -> [x])

declaration.AddProduction(functionDeclaration).SetReduceToFirst()

typeSpec.AddProduction(voidKeyword).SetReduceFunction (fun x -> Ast.Void)
typeSpec.AddProduction(intKeyword).SetReduceToFirst()

functionDeclaration.AddProduction(typeSpec, identifier, openParen, parameters, closeParen, compoundStatement)
    .SetReduceFunction (fun u v w x y z -> Ast.FunctionDeclaration(u, v, x, z))

parameters.AddProduction(parameterList).SetReduceToFirst()
parameters.AddProduction(voidKeyword).SetReduceFunction (fun x -> [])

parameterList.AddProduction(parameterList, parameter).SetReduceFunction (fun x y -> x @ [y])
parameterList.AddProduction(parameter)               .SetReduceFunction (fun x -> [x])

parameter.AddProduction(typeSpec, identifier).SetReduceFunction (fun x y -> Ast.ScalarParameter(x, y))

optionalStatementList.AddProduction(statementList).SetReduceToFirst()
optionalStatementList.AddProduction().SetReduceFunction (fun () -> [])

statementList.AddProduction(statementList, statement).SetReduceFunction (fun x y -> x @ [y])
statementList.AddProduction(statement)               .SetReduceFunction (fun x -> [x])

statement.AddProduction(expressionStatement).SetReduceFunction (fun x -> Ast.ExpressionStatement x)
statement.AddProduction(returnStatement)    .SetReduceFunction (fun x -> Ast.ReturnStatement x)

expressionStatement.AddProduction(expression, semicolon).SetReduceFunction (fun x y -> Ast.Expression x)
expressionStatement.AddProduction(semicolon)            .SetReduceFunction (fun x -> Ast.Nop)

compoundStatement.AddProduction(openCurly, optionalStatementList, closeCurly)
    .SetReduceFunction (fun x y z -> (None, y))

returnStatement.AddProduction(returnKeyword, expression, semicolon).SetReduceFunction (fun x y z -> Some y)
returnStatement.AddProduction(returnKeyword, semicolon)            .SetReduceFunction (fun x y -> None)

expression.AddProduction(expression, plus, expression).SetReduceFunction (fun x y z -> Ast.BinaryExpression(x, Ast.Add, z))
expression.AddProduction(expression, asterisk, expression).SetReduceFunction (fun x y z -> Ast.BinaryExpression(x, Ast.Multiply, z))
expression.AddProduction(identifier).SetReduceFunction (fun x -> Ast.IdentifierExpression x)
expression.AddProduction(number).SetReduceFunction (fun x -> Ast.LiteralExpression x)

configurator.LeftAssociative(downcast plus.Symbol) |> ignore
configurator.LeftAssociative(downcast asterisk.Symbol) |> ignore

configurator.LexerSettings.Ignore <- [|@"\s+"|]
let parser = configurator.CreateParser()

let parse (s : string) = parser.Parse(s)