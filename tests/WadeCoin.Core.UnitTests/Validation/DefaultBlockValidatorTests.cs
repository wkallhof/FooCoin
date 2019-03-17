using Moq;
using WadeCoin.Core.Models;
using WadeCoin.Core.UnitTests.Builders;
using WadeCoin.Core.Validation;
using Xunit;

namespace WadeCoin.Core.UnitTests.Validation
{
    public class DefaultBlockValidatorTests
    {
        private FakeCrypto _fakeCrypto;
        private Mock<ITransactionValidator> _fakeTransactionValidator;
        private Bogus.Faker _faker;

        public DefaultBlockValidatorTests()
        {
            _fakeCrypto = new FakeCrypto();
            _faker = new Bogus.Faker();
            _fakeTransactionValidator = new Mock<ITransactionValidator>();
            _fakeTransactionValidator.Setup(x => x.IsBlockTransactionValid(It.IsAny<Transaction>(), It.IsAny<BlockChain>())).Returns(true);
        }

        /// <summary>
        /// Make sure that the block's hash meets the difficulty set in the block.
        /// </summary>
        [Fact]
        public void HasValidHeader_ReturnsInvalid_IfHashedBlockDoesntMeetDifficulty(){
            var block = new BlockBuilder().WithHash(_faker.Random.Hash()).Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(block.Hash);

            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.HasValidHeader(block);

            Assert.False(result.IsValid);
            Assert.Equal(BlockValidationMessage.BlockHashDoesNotMeetDifficulty, result.Error);
        }

        /// <summary>
        /// Make sure that the hash is actually the hash of the block. This along with ensuring that
        /// the hash meets the difficulty is how miner's show proof of work.
        /// </summary>
        [Fact]
        public void HasValidHeader_ReturnsInvalid_IfBlockHashIsNotValid(){
            var block = new BlockBuilder().Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(_faker.Random.Hash());
            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.HasValidHeader(block);

            Assert.False(result.IsValid);
            Assert.Equal(BlockValidationMessage.BlockHashInvalid, result.Error);
        }
        
        /// <summary>
        /// Make sure we actually do return valid if things are OK
        /// </summary>
        [Fact]
        public void HasValidHeader_ReturnsValid_IfHeaderIsValid(){
            var block = new BlockBuilder().Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(block.Hash);
            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.HasValidHeader(block);

            Assert.True(result.IsValid);
        }

        /// <summary>
        /// Make sure to indicate when a null block was passed in
        /// </summary>
        [Fact]
        public void IsValidBlock_ReturnsInvalid_IfBlockIsNull(){
            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.IsValidBlock(null, new BlockChain());

            Assert.False(result.IsValid);
            Assert.Equal(BlockValidationMessage.BlockWasNull, result.Error);
        }
        
        /// <summary>
        /// Make sure the block has a transaction
        /// </summary>
        [Fact]
        public void IsValidBlock_ReturnsInvalid_IfTransactionIsNull(){
            var block = new BlockBuilder().WithTransaction(null).Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(block.Hash);
            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.IsValidBlock(block, new BlockChain());

            Assert.False(result.IsValid);
            Assert.Equal(BlockValidationMessage.TransactionWasNull, result.Error);
        }
        
        /// <summary>
        /// If the block's transaction is not valid, the block is not valid
        /// </summary>
        [Fact]
        public void IsValidBlock_ReturnsInvalid_IfTransactionIsInvalid(){
            var block = new BlockBuilder().Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(block.Hash);
            var invalidMessage = _faker.Lorem.Sentence();
            _fakeTransactionValidator.Setup(x => x.IsBlockTransactionValid(block.Transaction, It.IsAny<BlockChain>()))
                .Returns(ValidationResult.Invalid(invalidMessage));

            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.IsValidBlock(block, new BlockChain());

            Assert.False(result.IsValid);
            Assert.Equal(invalidMessage, result.Error);
        }
        
        /// <summary>
        /// Make sure we actually return valid if things are OK
        /// </summary>
        [Fact]
        public void IsValidBlock_ReturnsValid_IfBlockIsValid(){
            var block = new BlockBuilder().Build();
            _fakeCrypto.Setup(x => x.Hash(block)).Returns(block.Hash);
            var blockValidator = new DefaultBlockValidator(_fakeCrypto.Object, _fakeTransactionValidator.Object);

            var result = blockValidator.IsValidBlock(block, new BlockChain());

            Assert.True(result.IsValid);
        }

        // [Fact]
        // public void IsValidBlock_ReturnsInvalid_IfPreviousBlockHashDoesntReferencePreviousBlock(){

        // }
        
        // [Fact]
        // public void IsValidBlock_ReturnsInvalid_IfAssignedDifficultyIsInvalid(){

        // }

        // [Fact]
        // public void IsValidBlock_ReturnsInvalid_IfTimeStampIsGreaterThan2HoursInTheFuture(){

        // }
    }
}