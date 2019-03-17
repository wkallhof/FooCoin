using System.Collections.Generic;
using System.Linq;
using Moq;
using FooCoin.Core.Models;
using FooCoin.Core.UnitTests.Builders;
using FooCoin.Core.Validation;
using Xunit;

namespace FooCoin.Core.UnitTests.Validation
{
    public class DefaultBlockchainValidatorTests
    {
        private Mock<IBlockValidator> _fakeBlockValidator;
        private Bogus.Faker _faker;

        public DefaultBlockchainValidatorTests(){
            _faker = new Bogus.Faker();

            _fakeBlockValidator = new Mock<IBlockValidator>();
            _fakeBlockValidator.Setup(x => x.IsValidBlock(It.IsAny<Block>(), It.IsAny<Blockchain>())).Returns(true);
        }

        /// <summary>
        /// Assure that we are checking if the blockchain data structure is complete
        /// and that it or its list isn't null
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_WhenBlocksCollectionIsNull() {
            var blockchain = new BlockchainBuilder().WithBlocks((List<BlockBuilder>)null).Build();
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockchainValidationMessage.BlockchainIsNullOrEmpty, result.Error);
        }

        /// <summary>
        /// Assure that we are checking if the blockchain data structure is complete
        /// and that it has blocks
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_WhenBlocksCollectionIsEmpty() {
            var blockchain = new BlockchainBuilder().WithBlocks(new List<BlockBuilder>()).Build();
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockchainValidationMessage.BlockchainIsNullOrEmpty, result.Error);
        }

        /// <summary>
        /// If the blockchain only contains the genesis block (the first block),
        /// just return valid. There's really nothing to validate.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsValid_IfThereIsOnlyOneBlock(){
            var blockchain = new BlockchainBuilder().WithValidBlocks(1).Build();
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.True(result.IsValid);
        }

        /// <summary>
        /// When doing individual block validation, we need to skip the genesis block because as
        /// the first block, it isn't structurally complete. It doesn't have inputs and it won't
        /// have a previous block hash reference.
        /// </summary>
        [Fact]
        public void IsValid_SkipsGenesisBlock_WhenDoingIndividualBlockValidation() {
            var blockchain = new BlockchainBuilder().WithValidBlocks(2).Build();
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            _fakeBlockValidator.Verify(x => x.IsValidBlock(It.IsAny<Block>(), blockchain), Times.Once);
            _fakeBlockValidator.Verify(x => x.IsValidBlock(blockchain.Blocks.ElementAt(1), blockchain), Times.Once);
            _fakeBlockValidator.Verify(x => x.IsValidBlock(blockchain.Blocks.ElementAt(0), blockchain), Times.Never);
        }

        /// <summary>
        /// We need to ensure that each block in the blockchain
        /// is valid (except for the first genesis block). If *any* block is invalid, the entire
        /// blockchain is invalid.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_IfAnyBlockIsInvalid() {
             var blockchain = new BlockchainBuilder().WithValidBlocks(2).Build();
            _fakeBlockValidator.Setup(x => x.IsValidBlock(It.IsAny<Block>(), blockchain)).Returns(ValidationResult.Invalid(_faker.Lorem.Sentence()));
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockchainValidationMessage.NotAllBlocksAreValid, result.Error);
        }

        /// <summary>
        /// If any of the blocks in the blockchain are in the wrong order (their previous block hash isn't actually
        /// the previous block's hash), the blockchain is invalid.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_IfBlocksAreNotLinked() {
            var blockchain = new BlockchainBuilder().WithBlocks(
                new BlockBuilder(),
                new BlockBuilder()
            ).Build();

            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockchainValidationMessage.NotAllBlocksAreLinked, result.Error);
        }
        
        /// <summary>
        /// Make sure we actually return valid if things are valid
        /// </summary>
        [Fact]
        public void IsValid_ReturnsValid_WhenBlockchainIsValid() {
            var blockchain = new BlockchainBuilder().WithValidBlocks(2).Build();
            var blockchainValidator = new DefaultBlockchainValidator(_fakeBlockValidator.Object);

            var result = blockchainValidator.IsValid(blockchain);

            Assert.True(result.IsValid);
        }
    }
}