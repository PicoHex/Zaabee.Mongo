using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Xunit;

namespace Zaabee.Extensions.Mongo.UnitTest
{
    //TODO:BSON doesn't have an unsigned 32 bit integer type.
    public class UIntUnitTest : BaseUnitTest
    {
        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(0)]
        [InlineData(uint.MaxValue / 2)]
        public void UIntTest(uint value)
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            collection.InsertOne(testModel);
            var result = collection.UpdateMany(
                t => t.Id == testModel.Id,
                () => new TestModel { UInt = value }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.Equal(value, modifyModel.UInt);
            Assert.Equal(1L, collection.DeleteOne(t => t.Id == testModel.Id).DeletedCount);
        }

        [Fact]
        public void UIntArrayTest()
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            collection.InsertOne(testModel);
            var uintArray = new[] { uint.MinValue, uint.MaxValue / 2 };
            var result = collection.UpdateMany(
                t => t.Id == testModel.Id,
                () => new TestModel { UIntArray = uintArray }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.True(Comparer.Compare(uintArray, modifyModel.UIntArray));
            Assert.Equal(1L, collection.DeleteOne(t => t.Id == testModel.Id).DeletedCount);
        }

        [Fact]
        public void UIntListTest()
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            collection.InsertOne(testModel);
            var uintList = new List<uint> { uint.MinValue, uint.MaxValue / 2 };
            var result = collection.UpdateMany(
                t => t.Id == testModel.Id,
                () => new TestModel { UIntList = uintList }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.True(Comparer.Compare(uintList, modifyModel.UIntList));
            Assert.Equal(1L, collection.DeleteOne(t => t.Id == testModel.Id).DeletedCount);
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(0)]
        [InlineData(uint.MaxValue / 2)]
        public async Task UIntTestAsync(uint value)
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            await collection.InsertOneAsync(testModel);
            var result = await collection.UpdateManyAsync(
                t => t.Id == testModel.Id,
                () => new TestModel { UInt = value }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.Equal(value, modifyModel.UInt);
            Assert.Equal(
                1L,
                (await collection.DeleteOneAsync(t => t.Id == testModel.Id)).DeletedCount
            );
        }

        [Fact]
        public async Task UIntArrayTestAsync()
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            await collection.InsertOneAsync(testModel);
            var uintArray = new[] { uint.MinValue, uint.MaxValue / 2 };
            var result = await collection.UpdateManyAsync(
                t => t.Id == testModel.Id,
                () => new TestModel { UIntArray = uintArray }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.True(Comparer.Compare(uintArray, modifyModel.UIntArray));
            Assert.Equal(
                1L,
                (await collection.DeleteOneAsync(t => t.Id == testModel.Id)).DeletedCount
            );
        }

        [Fact]
        public async Task UIntListTestAsync()
        {
            var collection = MongoDatabase.GetCollection<TestModel>("TestModel");
            var testModel = TestModelFactory.GetModel();
            await collection.InsertOneAsync(testModel);
            var uintList = new List<uint> { uint.MinValue, uint.MaxValue / 2 };
            var result = await collection.UpdateManyAsync(
                t => t.Id == testModel.Id,
                () => new TestModel { UIntList = uintList }
            );
            Assert.Equal(1L, result.ModifiedCount);
            var modifyModel = collection.AsQueryable().First(p => p.Id == testModel.Id);
            Assert.True(Comparer.Compare(uintList, modifyModel.UIntList));
            Assert.Equal(
                1L,
                (await collection.DeleteOneAsync(t => t.Id == testModel.Id)).DeletedCount
            );
        }
    }
}
