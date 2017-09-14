using System;

namespace TrueSync
{
	public abstract class ChecksumExtractor
	{
		private static WorldChecksumExtractor worldExtractor;

		protected IPhysicsManagerBase physicsManager;

		protected abstract string GetChecksum();

		public ChecksumExtractor(IPhysicsManagerBase physicsManager)
		{
			this.physicsManager = physicsManager;
		}

		public static void Init(IPhysicsManagerBase physicsManager)
		{
			worldExtractor = new WorldChecksumExtractor(physicsManager);
		}

		public static string GetEncodedChecksum()
		{
			return Utils.GetMd5Sum(worldExtractor.GetChecksum());
		}
	}
}
