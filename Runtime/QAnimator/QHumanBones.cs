using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QHumanBones:MonoBehaviour
	{

		public List<Transform> bones = new List<Transform>();
		[QName("刷新人形骨骼")]
		private void Reset()
		{
			var avatar = GetComponent<Animator>().avatar;
			if (avatar == null || !avatar.isHuman) return;
			var boneNames = System.Enum.GetNames(typeof(QHumanBone));
			bones.Clear();
			var boneDatas = avatar.humanDescription.human;
			foreach (var boneName in boneNames)
			{
				var boneData= boneDatas.FirstOrDefault((data) => data.humanName == boneName);
				Transform bone = null;
				if (boneData.humanName == boneName)
				{
					bone=transform.FindAll(boneData.boneName);
					if (bone == null)
					{
						Debug.LogError(nameof(QHumanBones) +"未找到骨骼[" + boneData.boneName + "][" + boneData.humanName + "]");
					}
				}
				else
				{
					Debug.LogWarning(nameof(QHumanBones) + "未找到人体[" + boneName + "]对应的骨骼");
				}
				bones.Add(bone);
			}

		}
	}
	public enum QHumanBone
	{
		Hips,
		Spine,
		Chest,
		UpperChest,
		Neck,
		Head,
		LeftShoulder,
		LeftUpperArm,
		LeftLowerArm,
		LeftHand,
		RightShoulder,
		RightUpperArm,
		RightLowerArm,
		RightHand,
		LeftUpLeg,
		LeftLowerLeg,
		LeftFoot,
		RightUpLeg,
		RightLowerLeg,
		RightFoot,
	}
}

