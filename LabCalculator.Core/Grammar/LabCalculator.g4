// Граматика виразів для ЛР1, варіант 8 (за зразком з матеріалу «Калькулятор з використанням ANTLR4»).
// Операції: бінарні + - * /, унарні + -, ^ (степінь), inc(x), dec(x).
// Порядок альтернатив у правилі expression задає пріоритет: що вище — то сильніше.
grammar LabCalculator;

/*
 * Parser Rules
 */

compileUnit : expression EOF ;

expression
	: LPAREN expression RPAREN                                  # ParenthesizedExpr
	| operatorToken=(INC | DEC) LPAREN expression RPAREN        # IncDecExpr
	| operatorToken=(ADD | SUBTRACT) expression                 # UnaryExpr
	| <assoc=right> expression EXPONENT expression              # ExponentialExpr
	| expression operatorToken=(MULTIPLY | DIVIDE) expression   # MultiplicativeExpr
	| expression operatorToken=(ADD | SUBTRACT) expression      # AdditiveExpr
	| NUMBER                                                    # NumberExpr
	| IDENTIFIER                                                # IdentifierExpr
	| DELETED_REFERENCE                                         # DeletedReferenceExpr
	;

/*
 * Lexer Rules
 */

INC : 'inc' ;
DEC : 'dec' ;
NUMBER : INT ;
IDENTIFIER : [a-zA-Z]+ [1-9] [0-9]* ;
fragment INT : ('0'..'9')+ ;

// Посилання на видалену клітинку: ним програма замінює адресу, коли видаляють її рядок чи стовпчик.
DELETED_REFERENCE : '#ПОСИЛАННЯ!' ;

EXPONENT : '^' ;
MULTIPLY : '*' ;
DIVIDE : '/' ;
SUBTRACT : '-' ;
ADD : '+' ;
LPAREN : '(' ;
RPAREN : ')' ;

WS : [ \t\r\n]+ -> channel(HIDDEN) ;
