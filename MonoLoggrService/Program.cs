using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using System.Configuration;
using Microsoft.ProjectOxford.Vision.Contract;
using System.Diagnostics;

namespace MonoLoggrService
{
	class Program
	{
		static void Main(string[] args)
		{

			string loadDir = args.Length == 1 ? args[0] : keys.Default.local_load_dir;
			Console.WriteLine($"Loading images from {loadDir}...");

			var files = Directory.GetFiles(loadDir);

			Console.WriteLine($"Found {files.Length} images");





			var t = AnalyzePhotos(files);
			t.Wait();

			Console.WriteLine("Done");

		}

		static async Task AnalyzeSinglePhoto(string file)
		{
			VisionServiceClient VisionServiceClient = new VisionServiceClient(keys.Default.ms_cog_serv_api_key);

			using (Stream imageFileStream = File.OpenRead(file))
			{
				//
				// Analyze the image for all visual features
				//
				Debug.WriteLine($"Calling VisionServiceClient.AnalyzeImageAsync({file})...");
				VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
				AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);

				string resultTxt = (analysisResult.Categories.Where(cat => cat.Score > 0.1).FirstOrDefault()?.Name ?? "(no cat)")
					+ ", " + string.Join(", ", analysisResult.Tags.Where(tag => tag.Confidence > 0.1).Take(3).Select(tag => tag.Name))
					+ ": " + analysisResult.Description.Captions.FirstOrDefault()?.Text ?? "(no description)";

				if (analysisResult.Description.Captions.Max(cap => cap.Confidence) > 0.1)
					Console.WriteLine(resultTxt);

				Debug.WriteLine($"Task completed for {file}");
			}
		}


		static async Task AnalyzePhotos(IEnumerable<string> filenames)
		{
			List<Task> tasks = new List<Task>();

			foreach (var file in filenames)
			{
				Task t = Task.Run(() => AnalyzeSinglePhoto(file));
				tasks.Add(t);
				
				Debug.WriteLine($"Task added for {file}");

				await Task.Delay(500);
			}

			await Task.WhenAll(tasks);

			foreach (var ex in tasks.Where(task => task.IsFaulted).Select(task => task.Exception))
			{
				Console.WriteLine(ex.ToString());
			}
			
		}
	}
}
