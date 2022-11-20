[TOC]

# Array Texture

Inside the array texture ClusteringTextures.rtArr the following data layout is used:


\f{equation}{
\begin{aligned}
	\boldsymbol{T}_k(\delta)       & = \begin{bmatrix}
		\tilde{C}_2(\delta) w(\delta,k) f(\delta) \\
		\tilde{C}_3(\delta) w(\delta,k) f(\delta) \\
		z_k(\delta) \\
		w(\delta,k) f(\delta)
	\end{bmatrix}
\end{aligned}
\f}

|Variable|Explanation|
|----|----|
|\\(\boldsymbol{T}_k\\)|4-component floating point texture corresponding to \\(k\\)-th cluster|
|\\(\delta\\)|pixel|
|\\(\boldsymbol{\tilde{C}}(\delta)\\)|[color representation](#color-representation) used for material clustering|
|\\(w(\delta,k)\\)|weight of \\(k\\)-th cluster for the pixel \\(\delta\\) (1 or 0 in the case of K-Means)<SUP>&lowast;</SUP>|
|\\(f(\delta)\\)|[binary flag](#noise-flag) which determines if the pixel is rejected as noise|
|\\(z_k(\delta)\\)|used for calculating MSE ([see below](#third-component))|

<SUP>&lowast;</SUP> For [KHMp](#ClusteringAlgorithms.DispatcherKHMp) algorithm, weights \\(w(\delta,k)\\) for the given pixel do not add up to 1.

## Color representation {#color-representation}

Color representation \\(\boldsymbol{\tilde{C}}(\delta)\\) is generated from the RGB color of pixel \\(\delta\\) as follows:

1. maximize HSI saturation (intensity becomes invalid)
\f{equation}{
	\dot{\boldsymbol{C}} (\delta) = \boldsymbol{C}(\delta) - (1,1,1)^T \cdot \min\limits_{i \in \lbrace\mathrm{R,G,B}\rbrace}C_i(\delta)
\f}
2. normalize HSI intensity to \\(\frac{1}{3}\\)
\f{equation}{
\ddot{\boldsymbol{C}} (\delta) = \frac{
	\dot{\boldsymbol{C}} (\delta)
}{
	\Vert \dot{\boldsymbol{C}} (\delta) \Vert_1
}
\f}
3. convert from RGB to I<SUB>1</SUB>,I<SUB>2</SUB>,I<SUB>3</SUB> color space [1,2]
\f{equation}{
\boldsymbol{\tilde{C}} = \begin{bmatrix}
	\phantom{-}\frac{1}{3}\hspace{10px}\phantom{-} 	& \frac{1}{3}\hspace{10px} 	& \phantom{-}\frac{1}{3} \\[4pt]
	\phantom{-}\frac{1}{2}\hspace{10px}\phantom{-} 	& 0\hspace{10px} 			& -\frac{1}{2} \\[4px]
	-\frac{1}{4}\hspace{10px}\phantom{-} 			& \frac{1}{2}\hspace{10px} 	& -\frac{1}{4}
\end{bmatrix} \cdot \ddot{\boldsymbol{C}} (\delta)
\f}


### Code

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

## Noisy pixels {#noise-flag}

When HSI intensity and/or saturation is low, the hue becomes unstable. Such pixels will be ignored during clustering by setting \\(f(\delta)=0\\). The value of \\(f(\delta)\\) for the given RGB color is determined as follows:

\f{equation}{
f(\delta) = \begin{cases}
	1, & \text{if} \quad \left\Vert \boldsymbol{C}(\delta) - (1,1,1)^T \cdot \min\limits_{i \in \lbrace\mathrm{R,G,B}\rbrace}C_i(\delta) \right\Vert_1 \geq t \\
	0 & \text{otherwise}
	\end{cases}
\f}

|Variable|Explanation|
|----|----|
|\\(\boldsymbol{C}(\delta)\\)|RGB color of pixel \\(\delta\\)|
|\\(t\\)|threshold for determining noisy pixels (we used \\(t=0.05\\))

### Code

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

After the clustering is completed, noisy pixels are assigned to the nearest cluster. Reported MSE measurements ignore the noisy pixels.

## Third component {#third-component}

The third component \\(z_k(\delta)\\) has different values in different \\(\boldsymbol{T}_k\\) textures:

\f{equation}{
z_k(\delta) =
	\begin{cases}
	d_\mathrm{min}(\delta)^2 f(\delta) & \text{first texture } (k=1) \\
	f(\delta), & \text{second texture } (k=2) \\
	0 & \text{remaining textures}
	\end{cases}
\f}

|Variable|Explanation|
|----|----|
|\\(d_\mathrm{min}(\delta)\\)|Euclidean distance between the [color](#color-representation) \\(\tilde{C}_2(\delta),\tilde{C}_3(\delta)\\) and the nearest cluster center|
|\\(f(\delta)\\)|[binary flag](#noise-flag) which determines if the pixel is rejected as noise|

# Compute Buffer

Inside the [compute buffer](#ClusteringAlgorithms.ClusteringRTsAndBuffers.cbufClusterCenters), the following data layout is used:

|Component|Value|
|----|----|
|`x,y`|cluster center (see [color representation](#color-representation))|
|`z`|MSE (or -1, if MSE can not be computed)<SUP>&lowast;</SUP>|
|`w`|1 if cluster is not empty, 0 otherwise|

<SUP>&lowast;</SUP>If all pixels in the frame are [rejected as noise](#noise-flag) (i.e. \\(f(\delta)=0\\) for every \\(\delta\\)), MSE can not be computed.

# References

1. Kahu, S.Y., Raut, R.B., Bhurchandi, K.M.: Review and evaluation of color spaces
for image/video compression. Color Research & Application 44(1), 8–33 (2019) \f$\label{ref1}\f$
2. Ohta, Y.I., Kanade, T., Sakai, T.: Color information for region segmentation.
Computer graphics and image processing 13(3), 222–241 (1980)