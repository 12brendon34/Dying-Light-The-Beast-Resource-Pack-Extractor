using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MSH
{
    public static class GltfExporter
    {
        public static void DumpToGltf(MshData data, string fileName)
        {
            var gltf = new JsonObject();
            gltf["asset"] = new JsonObject
            {
                ["version"] = "2.0",
                ["generator"] = "MSH -> glTF exporter"
            };

            var buffer = new List<byte>();
            var bufferViews = new JsonArray();
            var accessors = new JsonArray();
            var meshes = new JsonArray();
            var nodes = new JsonArray();

            int accessorIndex = 0;
            int bufferViewIndex = 0;
            int meshIndex = 0;

            bool hasSkin = false;
            var boneNodeIndices = new int[(int)data.Root.NumNodes];
            for (int i = 0; i < boneNodeIndices.Length; ++i) boneNodeIndices[i] = -1;
            var inverseBindMatrices = new List<float[]>();

            for (int i = 0; i < (int)data.Root.NumNodes && i < (data.Tree?.Length ?? 0); ++i)
            {
                var tree = data.Tree[i];
                var node = tree.Node;

                if (node.Type != MshType.Bone)
                    continue;

                var mat4 = ConvertTo4x4(node.BoneTransform);
                inverseBindMatrices.Add(mat4);

                var matrixArray = new JsonArray();
                foreach (var f in mat4)
                    matrixArray.Add(f);

                var jointNode = new JsonObject
                {
                    ["name"] = node.Name ?? $"bone_{i}",
                    ["matrix"] = matrixArray
                };


                if (node.Parent != -1 && node.Parent != short.MaxValue)
                {
                    int parentIndex = boneNodeIndices[node.Parent];
                    if (parentIndex != -1)
                    {
                        if (!nodes[parentIndex].AsObject().ContainsKey("children"))
                            nodes[parentIndex].AsObject()["children"] = new JsonArray();
                        nodes[parentIndex].AsObject()["children"].AsArray().Add(nodes.Count);
                    }
                }

                boneNodeIndices[i] = nodes.Count;
                nodes.Add(jointNode);
            }

            if (inverseBindMatrices.Count > 0)
            {
                hasSkin = true;
                var ibmFlat = new List<float>();
                foreach (var m in inverseBindMatrices) ibmFlat.AddRange(m);

                int ibmOffset = buffer.Count;
                WriteFloatsToBuffer(buffer, ibmFlat.ToArray());

                var ibmBufferView = new JsonObject
                {
                    ["buffer"] = 0,
                    ["byteOffset"] = ibmOffset,
                    ["byteLength"] = ibmFlat.Count * sizeof(float)
                };
                bufferViews.Add(ibmBufferView);

                int ibmAccessorIndex = accessorIndex++;
                accessors.Add(new JsonObject
                {
                    ["bufferView"] = bufferViewIndex++,
                    ["componentType"] = 5126,
                    ["count"] = inverseBindMatrices.Count,
                    ["type"] = "MAT4"
                });

                var jointIndices = new List<int>();
                foreach (var idx in boneNodeIndices) if (idx != -1) jointIndices.Add(idx);
                
                var jointsArray = new JsonArray();
                foreach (var j in jointIndices)
                    jointsArray.Add(j);

                gltf["skins"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["joints"] = jointsArray,
                        ["inverseBindMatrices"] = ibmAccessorIndex,
                        ["skeleton"] = jointIndices.Count > 0 ? jointIndices[0] : 0
                    }
                };

            }

            for (int i = 0; i < (int)data.Root.NumNodes && i < (data.Tree?.Length ?? 0); ++i)
            {
                var tree = data.Tree[i];
                var node = tree.Node;
                var mesh = tree.Mesh != null && tree.Mesh.Length > 0 ? tree.Mesh[0] : null;

                if (node.Type != MshType.Mesh || mesh == null)
                    continue;

                float[] vertices;
                if (mesh.VxyzData0 != null)
                {
                    var floatArray = BytesToFloatArray(mesh.VxyzData0);
                    if (mesh.Vxyz[0].Fmt == MvFmt.Float4)
                    {
                        int vertexCount = (int)mesh.NumVertices;
                        vertices = new float[vertexCount * 3];
                        for (int v = 0; v < vertexCount; ++v)
                        {
                            vertices[v * 3 + 0] = floatArray[v * 4 + 0];
                            vertices[v * 3 + 1] = floatArray[v * 4 + 1];
                            vertices[v * 3 + 2] = floatArray[v * 4 + 2];
                        }
                    }
                    else if (mesh.Vxyz[0].Fmt == MvFmt.Float3)
                    {
                        vertices = new float[mesh.NumVertices * 3];
                        Array.Copy(floatArray, 0, vertices, 0, vertices.Length);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unsupported v_xyz format for node {node.Name}");
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping mesh for node {node.Name}: no managed vertex buffer available.");
                    continue;
                }

                ushort[] indicesArr = mesh.Indices ?? Array.Empty<ushort>();
                var indicesList = new List<ushort>(indicesArr);

                
                var normals = new float[vertices.Length];
                for (int t = 0; t + 2 < indicesList.Count; t += 3)
                {
                    int i0 = indicesList[t];
                    int i1 = indicesList[t + 1];
                    int i2 = indicesList[t + 2];

                    var p0 = new System.Numerics.Vector3(vertices[i0 * 3 + 0], vertices[i0 * 3 + 1], vertices[i0 * 3 + 2]);
                    var p1 = new System.Numerics.Vector3(vertices[i1 * 3 + 0], vertices[i1 * 3 + 1], vertices[i1 * 3 + 2]);
                    var p2 = new System.Numerics.Vector3(vertices[i2 * 3 + 0], vertices[i2 * 3 + 1], vertices[i2 * 3 + 2]);

                    var n = System.Numerics.Vector3.Cross(p1 - p0, p2 - p0);
                    if (n != System.Numerics.Vector3.Zero) n = System.Numerics.Vector3.Normalize(n);

                    normals[i0 * 3 + 0] += n.X; normals[i0 * 3 + 1] += n.Y; normals[i0 * 3 + 2] += n.Z;
                    normals[i1 * 3 + 0] += n.X; normals[i1 * 3 + 1] += n.Y; normals[i1 * 3 + 2] += n.Z;
                    normals[i2 * 3 + 0] += n.X; normals[i2 * 3 + 1] += n.Y; normals[i2 * 3 + 2] += n.Z;
                }
                
                int vcount = (int)mesh.NumVertices;
                for (int v = 0; v < vcount; ++v)
                {
                    var nv = new System.Numerics.Vector3(normals[v * 3 + 0], normals[v * 3 + 1], normals[v * 3 + 2]);
                    if (nv != System.Numerics.Vector3.Zero) nv = System.Numerics.Vector3.Normalize(nv);
                    normals[v * 3 + 0] = nv.X; normals[v * 3 + 1] = nv.Y; normals[v * 3 + 2] = nv.Z;
                }

                var texcoords = new List<float>();
                if (mesh.VUvData0 != null && mesh.VUvData0.Length >= mesh.NumVertices * 8)
                {
                    var uvFloats = BytesToFloatArray(mesh.VUvData0);
                    for (int v = 0; v < (int)mesh.NumVertices; ++v)
                    {
                        float u = uvFloats[v * 2 + 0];
                        float vcoord = uvFloats[v * 2 + 1];
                        texcoords.Add(u);
                        texcoords.Add(1.0f - vcoord);
                    }
                }

                int WriteFloatBuffer(float[] data, string type, int components)
                {
                    int offset = buffer.Count;
                    WriteFloatsToBuffer(buffer, data);
                    bufferViews.Add(new JsonObject
                    {
                        ["buffer"] = 0,
                        ["byteOffset"] = offset,
                        ["byteLength"] = data.Length * sizeof(float)
                    });
                    accessors.Add(new JsonObject
                    {
                        ["bufferView"] = bufferViewIndex++,
                        ["componentType"] = 5126, // FLOAT
                        ["count"] = data.Length / components,
                        ["type"] = type
                    });
                    return accessorIndex++;
                }

                int vertAccessor = WriteFloatBuffer(vertices, "VEC3", 3);
                int normAccessor = WriteFloatBuffer(normals, "VEC3", 3);
                int texAccessor = -1;
                if (texcoords.Count > 0)
                {
                    texAccessor = WriteFloatBuffer(texcoords.ToArray(), "VEC2", 2);
                }
                
                int indexOffset = buffer.Count;
                WriteUShortsToBuffer(buffer, indicesList.ToArray());
                bufferViews.Add(new JsonObject
                {
                    ["buffer"] = 0,
                    ["byteOffset"] = indexOffset,
                    ["byteLength"] = indicesList.Count * sizeof(ushort)
                });
                int indexAccessor = accessorIndex++;
                accessors.Add(new JsonObject
                {
                    ["bufferView"] = bufferViewIndex++,
                    ["componentType"] = 5123,
                    ["count"] = indicesList.Count,
                    ["type"] = "SCALAR"
                });

                var primitiveAttributes = new JsonObject
                {
                    ["POSITION"] = vertAccessor,
                    ["NORMAL"] = normAccessor
                };
                if (texAccessor != -1)
                    primitiveAttributes["TEXCOORD_0"] = texAccessor;

                var primitive = new JsonObject
                {
                    ["attributes"] = primitiveAttributes,
                    ["indices"] = indexAccessor
                };

                meshes.Add(new JsonObject
                {
                    ["primitives"] = new JsonArray(primitive),
                    ["name"] = node.Name ?? $"mesh_{i}"
                });

                // Mesh node
                var meshNode = new JsonObject
                {
                    ["mesh"] = meshIndex++,
                    ["name"] = node.Name ?? $"meshNode_{i}"
                };
                if (hasSkin)
                    meshNode["skin"] = 0;

                nodes.Add(meshNode);
            }

            // Scene assembly
            gltf["buffers"] = new JsonArray(new JsonObject
            {
                ["byteLength"] = buffer.Count,
                ["uri"] = fileName + ".bin"
            });
            gltf["bufferViews"] = bufferViews;
            gltf["accessors"] = accessors;
            gltf["meshes"] = meshes;
            gltf["nodes"] = nodes;
            gltf["scenes"] = new JsonArray(new JsonObject { ["nodes"] = new JsonArray() });

            // add all nodes to scene[0].nodes
            var sceneNodes = gltf["scenes"].AsArray()[0].AsObject()["nodes"].AsArray();
            for (int i = 0; i < nodes.Count; ++i) sceneNodes.Add(i);
            gltf["scene"] = 0;

            System.IO.File.WriteAllBytes(fileName + ".bin", buffer.ToArray());

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonText = gltf.ToJsonString(options);
            System.IO.File.WriteAllText(fileName + ".gltf", jsonText);
        }

        private static float[] ConvertTo4x4(Matrix3X4 mtx)
        {
            var mat = new float[16];
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 4; ++c)
                    mat[r * 4 + c] = mtx[r, c];
            }
            mat[12] = 0f; mat[13] = 0f; mat[14] = 0f; mat[15] = 1f;
            return mat;
        }

        private static void WriteFloatsToBuffer(List<byte> buffer, float[] data)
        {
            foreach (var f in data)
            {
                var bytes = BitConverter.GetBytes(f);
                if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
                buffer.AddRange(bytes);
            }
        }

        private static void WriteFloatsToBuffer(List<byte> buffer, IEnumerable<float> data)
        {
            foreach (var f in data)
            {
                var bytes = BitConverter.GetBytes(f);
                if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
                buffer.AddRange(bytes);
            }
        }
        private static void WriteUShortsToBuffer(List<byte> buffer, ushort[] data)
        {
            foreach (var v in data)
            {
                Span<byte> b = stackalloc byte[2];
                BinaryPrimitives.WriteUInt16LittleEndian(b, v);
                buffer.Add(b[0]); buffer.Add(b[1]);
            }
        }

        private static float[] BytesToFloatArray(byte[] bytes)
        {
            int floats = bytes.Length / 4;
            var result = new float[floats];
            Buffer.BlockCopy(bytes, 0, result, 0, floats * 4);
            if (!BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < floats; ++i)
                {
                    var slice = new byte[4];
                    Buffer.BlockCopy(bytes, i * 4, slice, 0, 4);
                    Array.Reverse(slice);
                    result[i] = BitConverter.ToSingle(slice, 0);
                }
            }
            return result;
        }
    }
}
