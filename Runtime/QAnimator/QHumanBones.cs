using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QHumanBones:MonoBehaviour
	{
		[SerializeField]
		private List<Transform> bones = new List<Transform>();
		private void Reset()
		{
			var avatar = GetComponent<Animator>().avatar;
			if (avatar == null || !avatar.isHuman) return;
			var boneNames = System.Enum.GetNames(typeof(QHumanBoneName));
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
		public Transform GetBone(QHumanBoneName name)
		{
			return bones[(int)name];
		}
	}
	public enum QHumanBoneName
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
		LeftUpperLeg,
		LeftLowerLeg,
		LeftFoot,
		RightUpperLeg,
		RightLowerLeg,
		RightFoot,
	}
}

