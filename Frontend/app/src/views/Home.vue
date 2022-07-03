<template>
  <div class="home">
    <b-container>
      <b-row> 
        <b-col cols="2"></b-col>
        <b-col cols="8">
          <b-card>
            <h4>Welcome to my Internet Services Architectures project!</h4>
            <hr/>
            <h5>In the <i>Users</i> tab You may find list of users served by the Java Spring backend at <b>localhost:8080/api/users/</b>.</h5>
            <h5><i>Cars</i> tab works in the same way but with cars (quite obvious)</h5>
            <h5>Feel free to test out some features like creating new objects and have fun!</h5>
          </b-card>
        </b-col>
        <b-col cols="2"></b-col>
      </b-row>
    </b-container>
  </div>
</template>

<script>
export default {
  name: "Home",
};
</script>
