using System.Collections.Generic;
using System.Linq;
using Moq;
using WadeCoin.Core.Models;
using WadeCoin.Core.UnitTests.Builders;
using WadeCoin.Core.Validation;
using Xunit;

namespace WadeCoin.Core.UnitTests.Validation
{
    public class DefaultBlockChainValidatorTests
    {
        private Mock<IBlockValidator> _fakeBlockValidator;
        private Bogus.Faker _faker;

        public DefaultBlockChainValidatorTests(){
            _faker = new Bogus.Faker();

            _fakeBlockValidator = new Mock<IBlockValidator>();
            _fakeBlockValidator.Setup(x => x.IsValidBlock(It.IsAny<Block>(), It.IsAny<BlockChain>())).Returns(true);
        }

        /// <summary>
        /// Assure that we are checking if the blockchain data structure is complete
        /// and that it or its list isn't null
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_WhenBlocksCollectionIsNull() {
            var blockChain = new BlockChainBuilder().WithBlocks((List<BlockBuilder>)null).Build();
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockChainValidationMessage.BlockChainIsNullOrEmpty, result.Error);
        }

        /// <summary>
        /// Assure that we are checking if the blockchain data structure is complete
        /// and that it has blocks
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_WhenBlocksCollectionIsEmpty() {
            var blockChain = new BlockChainBuilder().WithBlocks(new List<BlockBuilder>()).Build();
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockChainValidationMessage.BlockChainIsNullOrEmpty, result.Error);
        }

        /// <summary>
        /// If the blockchain only contains the genesis block (the first block),
        /// just return valid. There's really nothing to validate.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsValid_IfThereIsOnlyOneBlock(){
            var blockChain = new BlockChainBuilder().WithValidBlocks(1).Build();
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.True(result.IsValid);
        }

        /// <summary>
        /// When doing individual block validation, we need to skip the genesis block because as
        /// the first block, it isn't structurally complete. It doesn't have inputs and it won't
        /// have a previous block hash reference.
        /// </summary>
        [Fact]
        public void IsValid_SkipsGenesisBlock_WhenDoingIndividualBlockValidation() {
            var blockChain = new BlockChainBuilder().WithValidBlocks(2).Build();
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            _fakeBlockValidator.Verify(x => x.IsValidBlock(It.IsAny<Block>(), blockChain), Times.Once);
            _fakeBlockValidator.Verify(x => x.IsValidBlock(blockChain.Blocks.ElementAt(1), blockChain), Times.Once);
            _fakeBlockValidator.Verify(x => x.IsValidBlock(blockChain.Blocks.ElementAt(0), blockChain), Times.Never);
        }

        /// <summary>
        /// We need to ensure that each block in the blockchain
        /// is valid (except for the first genesis block). If *any* block is invalid, the entire
        /// blockchain is invalid.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_IfAnyBlockIsInvalid() {
             var blockChain = new BlockChainBuilder().WithValidBlocks(2).Build();
            _fakeBlockValidator.Setup(x => x.IsValidBlock(It.IsAny<Block>(), blockChain)).Returns(ValidationResult.Invalid(_faker.Lorem.Sentence()));
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockChainValidationMessage.NotAllBlocksAreValid, result.Error);
        }

        /// <summary>
        /// If any of the blocks in the blockchain are in the wrong order (their previous block hash isn't actually
        /// the previous block's hash), the blockchain is invalid.
        /// </summary>
        [Fact]
        public void IsValid_ReturnsInvalid_IfBlocksAreNotLinked() {
            var blockChain = new BlockChainBuilder().WithBlocks(
                new BlockBuilder(),
                new BlockBuilder()
            ).Build();

            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.False(result.IsValid);
            Assert.Equal(BlockChainValidationMessage.NotAllBlocksAreLinked, result.Error);
        }
        
        /// <summary>
        /// Make sure we actually return valid if things are valid
        /// </summary>
        [Fact]
        public void IsValid_ReturnsValid_WhenBlockChainIsValid() {
            var blockChain = new BlockChainBuilder().WithValidBlocks(2).Build();
            var blockChainValidator = new DefaultBlockChainValidator(_fakeBlockValidator.Object);

            var result = blockChainValidator.IsValid(blockChain);

            Assert.True(result.IsValid);
        }
    }
}