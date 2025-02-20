CREATE TABLE BankAccounts (
    AccountID INT NOT NULL PRIMARY KEY, 
    Balance BIGINT NOT NULL,             

    CONSTRAINT CK_BankAccounts_AccountID CHECK (AccountID BETWEEN 10000 AND 99999),
    CONSTRAINT CK_BankAccounts_Balance CHECK (Balance >= 0)
);
