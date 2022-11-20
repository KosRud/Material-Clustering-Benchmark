[TOC]

# ClusteringTextures.rtArr

Inside ClusteringTextures.rtArr the following data layout is used:

$$
\begin{aligned}
	\pmb{T}_k(\delta)       & = \begin{bmatrix}
		\mathit{\tilde{C}}_2(\delta) w(\delta,k) f(\delta) \\\\
		\mathit{\tilde{C}}_3(\delta) w(\delta,k) f(\delta) \\\\
		z_k(\delta) \\\\
		w(\delta,k) f(\delta)
	\end{bmatrix}
\end{aligned}
$$

|Variable|Explanation|
|----|----|
|\\(\pmb{T}_k\\)|4-component floating point texture corresponding to \\(k\\)-th cluster|
|\\(\delta\\)|pixel|
|\\(\pmb{\mathit{\tilde{C}}}(\delta)\\)|color representation used for material clustering (see below)|
|\\(w(\delta,k)\\)|weight of \\(k\\)-th cluster for the pixel \\(\delta\\) (1 or 0 in the case of K-Means)|
|\\(f(\delta)\\)|binary flag which determines if the pixel is rejected as noise|
|\\(z_k(\delta)\\)|see below|




## Color representation

Color representation \\(\mathit{\tilde{C}}_2(\delta),\mathit{\tilde{C}}_3(\delta)\\) is generated from the RGB color using the function `project()` in the shader:

~~~~~~~~~~~~~{cpp}
float2 project(float3 col)
{
	/* 
		maximize HSI saturation
		intensity becomes invalid
	*/
	col -= min(
		col.r,
		min(
			col.g,
			col.b
		)
	);

    /*
		normalize HSI intensity
		
		this creates potential division by zero
		erroneous pixels will be rejected due to is_color_valid() check
	*/
    col /= dot(col, 1);

	// transform to I1,I2,I3 color space
	col = mul(rgb_2_iii_matrix,col);

	// return chromaticity axes, ignore intensity
	return col.gb;
}
~~~~~~~~~~~~~

Matrix for \\(\mathrm{RGB} \Rightarrow \mathrm{I}_1\mathrm{I}_2\mathrm{I}_3\\) conversion [1]:

~~~~~~~~~~~~~{cpp}
static const float3x3 rgb_2_iii_matrix = {
	0.3333,	0.3333,	0.3333,
	0.5,	0,		-0.5,
	-0.25,	0.5,	-0.25
};
~~~~~~~~~~~~~

## Noisy pixels

When HSI intensity and/or saturation is low, the hue becomes unstable. Such pixels will be ignored during clustering by setting \\(f(\delta)=0\\). The value of \\(f(\delta)\\) for the given RGB color is determined by the function `is_color_valid()` in the shader:

~~~~~~~~~~~~~{cpp}
#define valid_threshold 0.05

bool is_color_valid(float3 col) {

	// subtract achromatic portion
	col -= min(
		col.r,
		min(
			col.g,
			col.b
		)
	);

	return dot(col, 1) > valid_threshold;
}
~~~~~~~~~~~~~

## Third component

The third component \\(z_k(\delta)\\) has different values in different textures:

$$
z_k(\delta) =
	\begin{cases}
	d_\mathrm{min}(\delta)^2 f(\delta) & \text{first texture } (k=1) \\\\
	f(\delta), & \text{second texture } (k=2) \\\\
	0 & \text{remaining textures}
	\end{cases}
$$

|Variable|Explanation|
|----|----|
|\\(d_\mathrm{min}(\delta)\\)|Euclidean distance between the color \\(\mathit{\tilde{C}}_2(\delta),\mathit{\tilde{C}}_3(\delta)\\) and the nearest cluster center|

# Compute Buffer

a

# References

1. Kahu, S.Y., Raut, R.B., Bhurchandi, K.M.: Review and evaluation of color spaces
for image/video compression. Color Research & Application 44(1), 8–33 (2019)
2. Ohta, Y.I., Kanade, T., Sakai, T.: Color information for region segmentation.
Computer graphics and image processing 13(3), 222–241 (1980)