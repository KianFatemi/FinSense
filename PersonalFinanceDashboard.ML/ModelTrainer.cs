using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.ML;
using PersonalFinanceDashboard.Models;

namespace PersonalFinanceDashboard.ML
{
    public class ModelTrainer
    {
        private readonly MLContext mLContext;
        public ModelTrainer()
        {
            mLContext = new MLContext(seed: 0);
        }

        // gets the designated file path for a users specific model
        private static string GetModelPathForUser(string userId)
        {
            string modelsDirectory = Path.Combine(AppContext.BaseDirectory, "MlModels");
            Directory.CreateDirectory(modelsDirectory);
            return Path.Combine(modelsDirectory, $"{userId}.zip");

        }

        public void TrainAndSaveModel(IEnumerable<Models.Transaction> userTransactions, string userId)
        {
            var trainingDataEnumerable = userTransactions
                .Where(t => !string.IsNullOrEmpty(t.Description) && !string.IsNullOrEmpty(t.Category))
                .Select(t => new TransactionData { Description = t.Description, Category = t.Category });

            if (!trainingDataEnumerable.Any())
            {
                System.Console.WriteLine("Not enough data to train a model");
                return;
            }

            IDataView trainingData = mLContext.Data.LoadFromEnumerable(trainingDataEnumerable);

            var pipeline = mLContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Category", outputColumnName: "Label")
                .Append(mLContext.Transforms.Text.FeaturizeText(inputColumnName: "Description", outputColumnName: "DescriptionFeaturized"))
                .Append(mLContext.Transforms.Concatenate("Features", "DescriptionFeaturized"))
                .Append(mLContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(mLContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var trainedModel = pipeline.Fit(trainingData);

            string modelPath = GetModelPathForUser(userId);
            mLContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
            Console.WriteLine($"Model for user {userId} saved to {modelPath}");
        }

        public ITransformer LoadModelForUser(string userId)
        {
            string modelPath = GetModelPathForUser(userId);
            if (!File.Exists(modelPath))
            {
                return null;
            }
            return mLContext.Model.Load(modelPath, out _);
        }
    }
}
