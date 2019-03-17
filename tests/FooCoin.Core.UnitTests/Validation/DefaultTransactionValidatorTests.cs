using System.Collections.Generic;
using System.Linq;
using Bogus;
using Moq;
using FooCoin.Core.Models;
using FooCoin.Core.Validation;
using Xunit;

namespace FooCoin.Core.UnitTests.Validation
{
    public class DefaultTransactionValidatorTests
    {
        public Mock<ICrypto> _fakeCrypto;
        public Randomizer _random;

        public DefaultTransactionValidatorTests(){
            _random = new Randomizer();

            _fakeCrypto = new Mock<ICrypto>();
            _fakeCrypto.Setup(x => x.Hash(It.IsAny<string>())).Returns((string input) => input.GetHashCode().ToString());
            _fakeCrypto.Setup(x => x.DoubleHash(It.IsAny<string>())).Returns((string input) => input.GetHashCode().ToString());
            _fakeCrypto.Setup(x => x.DoubleHash(It.IsAny<IHashable>())).Returns((IHashable input) => input.GetHashCode().ToString());
            _fakeCrypto.Setup(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        }

        [Fact]
        public void IsUnconfirmedTransactionValid_ReturnsInvalid_IfTransactionIsAlreadyInBlockchain(){
            var transaction = GetFilledOutTransaction();
            var blockchain = InitializeBlockchainWithTransactions(transaction);
            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsUnconfirmedTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.TransactionAlreadyInBlockchain, result.Error);
        }

        [Fact]
        public void IsBlockTransactionValid_ReturnsInvalid_IfTransactionIdIsNotHashOfTransaction(){
            var transaction = GetFilledOutTransaction();
            var blockchain = InitializeBlockchainWithTransactions(transaction);
            _fakeCrypto.Setup(x => x.DoubleHash(transaction)).Returns(_random.Hash());

            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsBlockTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.IdDoesNotMatchHash, result.Error);
        }

        [Fact]
        public void IsBlockTransactionValid_ReturnsInvalid_IfInputListIsEmpty(){
            var transaction = new Transaction(new List<Input>(), OutputFaker.Generate(1));
            transaction.Id = _random.Hash();
            var blockchain = InitializeBlockchainWithTransactions(transaction);
            _fakeCrypto.Setup(x => x.DoubleHash(transaction)).Returns(transaction.Id);

            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsBlockTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.TransactionMissingInputs, result.Error);
        }

        [Fact]
        public void IsBlockTransactionValid_ReturnsInvalid_IfOutputListIsEmpty(){
            var transaction = new Transaction(InputFaker.Generate(1), new List<Output>());
            transaction.Id = _random.Hash();
            var blockchain = InitializeBlockchainWithTransactions(transaction);
            _fakeCrypto.Setup(x => x.DoubleHash(transaction)).Returns(transaction.Id);

            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsBlockTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.TransactionMissingOutputs, result.Error);
        }

        [Fact]
        public void IsBlockTransactionValid_ReturnsInvalid_IfInputHasInvalidTransactionReference(){
            var firstTransaction = GetFilledOutTransaction();
            var transaction = GetFilledOutTransaction();
            var blockchain = InitializeBlockchainWithTransactions(firstTransaction, transaction);
            _fakeCrypto.Setup(x => x.DoubleHash(transaction)).Returns(transaction.Id);

            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsBlockTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.MatchingTransactionNotFoundForInput, result.Error);
        }

        [Fact]
        public void IsBlockTransactionValid_ReturnsInvalid_IfInputHasInvalidOutputIndex(){
            var firstTransaction = GetFilledOutTransaction();
            var invalidInput = new Input() { TransactionId = firstTransaction.Id, OutputIndex = firstTransaction.Outputs.Count };

            var transaction = new Transaction(new List<Input>(){invalidInput}, OutputFaker.Generate(1));
            transaction.Id = _random.Hash();
            var blockchain = InitializeBlockchainWithTransactions(firstTransaction, transaction);
            _fakeCrypto.Setup(x => x.DoubleHash(transaction)).Returns(transaction.Id);

            var validator = new DefaultTransactionValidator(_fakeCrypto.Object);

            var result = validator.IsBlockTransactionValid(transaction, blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(TransactionValidationMessage.InputsOutputIndexExceedsRange, result.Error);
        }

        
        //IsBlockTransactionValid_ReturnsInvalid_IfInputOutputPubKeyMismatch
        //IsBlockTransactionValid_ReturnsInvalid_IfInputSignatureInvalid
        //IsBlockTransactionValid_ReturnsInvalid_IfInputReferencesSpentOutput
        //IsBlockTransactionValid_ReturnsInvalid_IfMoneyInIsNotMoneyOut

        //IsBlockTransactionValid_ReturnsValid_IfTransactionIsValid
        //IsUnconfirmedTransactionValid_ReturnsValid_IfTransactionIsValid

        private Transaction GetFilledOutTransaction()
            => TransactionFaker.Generate();

        private Faker<Input> InputFaker
            => new Faker<Input>()
                .RuleFor(x => x.TransactionId, f => f.Random.Hash(10))
                .RuleFor(x => x.FullPubKey, f => f.Random.Hash())
                .RuleFor(x => x.Signature, f => f.Random.Hash(10))
                .RuleFor(x => x.OutputIndex, f => f.Random.Int(min: 0, max: 5));

        private Faker<Output> OutputFaker
            => new Faker<Output>()
                .RuleFor(x => x.Amount, f => f.Random.Decimal(min:(decimal)0.1))
                .RuleFor(x => x.PubKeyHash, f => f.Random.Hash(10));

        private Faker<Transaction> TransactionFaker
            => new Faker<Transaction>()
                .CustomInstantiator(f => new Transaction(InputFaker.Generate(f.Random.Int(min: 1, max: 5)), OutputFaker.Generate(f.Random.Int(min: 1, max: 5))))
                .RuleFor(x => x.Id, f => f.Random.Hash(10));

        private Blockchain InitializeBlockchainWithTransactions(params Transaction[] transactions){
            var random = new Randomizer();
            var blockchain = new Blockchain();

            transactions.ToList().ForEach(x => blockchain.Add(new Block(random.Hash(10), x)));

            return blockchain;
        }

    }
}